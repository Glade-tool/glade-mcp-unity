"""Tests for skill level calibration — local JSON persistence."""

import json

from gladekit_mcp.skill import (
    _MIN_MESSAGES,
    _last_persisted_count,
    _session_messages,
    load_skill_level,
    maybe_persist,
    record_message,
    should_persist_now,
    update_from_session,
)


def test_load_skill_level_missing_file(tmp_path):
    """No skill_level.json → returns None (not calibrated)."""
    result = load_skill_level(str(tmp_path))
    assert result is None


def test_load_skill_level_corrupt_json(tmp_path):
    """Corrupt JSON file → returns None without raising, logs a warning."""
    gladekit_dir = tmp_path / ".gladekit"
    gladekit_dir.mkdir()
    (gladekit_dir / "skill_level.json").write_text("{ not valid json !!!")

    result = load_skill_level(str(tmp_path))
    assert result is None  # degrades gracefully


def test_load_skill_level_too_few_samples(tmp_path):
    """File exists but sample_count < minimum → returns None."""
    gladekit_dir = tmp_path / ".gladekit"
    gladekit_dir.mkdir()
    (gladekit_dir / "skill_level.json").write_text(
        json.dumps(
            {
                "skill_level": "expert",
                "confidence": 0.9,
                "sample_count": _MIN_MESSAGES - 1,
            }
        )
    )

    result = load_skill_level(str(tmp_path))
    assert result is None


def test_load_skill_level_valid(tmp_path):
    """Valid file with enough samples → returns the stored level."""
    gladekit_dir = tmp_path / ".gladekit"
    gladekit_dir.mkdir()
    (gladekit_dir / "skill_level.json").write_text(
        json.dumps(
            {
                "skill_level": "intermediate",
                "confidence": 0.5,
                "sample_count": _MIN_MESSAGES + 5,
            }
        )
    )

    result = load_skill_level(str(tmp_path))
    assert result == "intermediate"


def test_load_skill_level_empty_json(tmp_path):
    """Empty JSON object → returns None."""
    gladekit_dir = tmp_path / ".gladekit"
    gladekit_dir.mkdir()
    (gladekit_dir / "skill_level.json").write_text("{}")

    result = load_skill_level(str(tmp_path))
    assert result is None


def test_load_skill_level_cloud_disabled(monkeypatch, tmp_path):
    """Cloud features off → local file still works."""
    monkeypatch.delenv("GLADEKIT_API_KEY", raising=False)
    gladekit_dir = tmp_path / ".gladekit"
    gladekit_dir.mkdir()
    (gladekit_dir / "skill_level.json").write_text(
        json.dumps(
            {
                "skill_level": "beginner",
                "confidence": 0.05,
                "sample_count": _MIN_MESSAGES + 2,
            }
        )
    )

    result = load_skill_level(str(tmp_path))
    assert result == "beginner"


# ── Persistence wiring ────────────────────────────────────────────────────────


def test_should_persist_now_below_threshold(tmp_path):
    """No messages → no persistence."""
    sid = "test-sid-1"
    assert should_persist_now(session_id=sid) is False
    record_message("hello", session_id=sid)
    record_message("world", session_id=sid)
    assert should_persist_now(session_id=sid) is False


def test_maybe_persist_writes_after_threshold(tmp_path):
    """Crossing _MIN_MESSAGES with expert vocabulary writes skill_level.json."""
    sid = "test-sid-persist"
    # Expert-flavored messages
    record_message("how do I use scriptableobject with a ienumerator coroutine?", session_id=sid)
    record_message("can I override a monobehaviour via abstract class?", session_id=sid)
    record_message("is a quaternion slerp better than lerp for camera follow?", session_id=sid)

    assert should_persist_now(session_id=sid) is True
    level = maybe_persist(str(tmp_path), session_id=sid)
    assert level in {"beginner", "intermediate", "expert"}

    # File exists and carries the level we just wrote
    skill_file = tmp_path / ".gladekit" / "skill_level.json"
    assert skill_file.exists()
    data = json.loads(skill_file.read_text())
    assert data["skill_level"] == level
    assert data["sample_count"] >= _MIN_MESSAGES


def test_maybe_persist_throttles_between_writes(tmp_path):
    """After the first write, no re-persist until another _PERSIST_EVERY_N messages."""
    sid = "test-sid-throttle"
    for text in ["scriptableobject rigidbody navmesh", "slerp mathf", "cinemachine vcam"]:
        record_message(text, session_id=sid)
    maybe_persist(str(tmp_path), session_id=sid)
    assert should_persist_now(session_id=sid) is False
    record_message("prefab variant shader graph", session_id=sid)
    assert should_persist_now(session_id=sid) is False
    record_message("addressables serialization", session_id=sid)
    record_message("animator controller blend tree", session_id=sid)
    assert should_persist_now(session_id=sid) is True


def test_update_from_session_is_per_session(tmp_path):
    """Messages in one session id don't bleed into another."""
    sid_a = "session-a"
    sid_b = "session-b"
    for _ in range(_MIN_MESSAGES):
        record_message("scriptableobject abstract override", session_id=sid_a)

    # sid_b has nothing
    assert update_from_session(str(tmp_path), session_id=sid_b) is None
    # sid_a persists cleanly
    assert update_from_session(str(tmp_path), session_id=sid_a) is not None
    # Re-cleanup for other tests (the autouse fixture in conftest.py handles it,
    # but be explicit for in-file readability).
    _session_messages.pop(sid_a, None)
    _session_messages.pop(sid_b, None)
    _last_persisted_count.pop(sid_a, None)
    _last_persisted_count.pop(sid_b, None)
