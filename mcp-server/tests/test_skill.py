"""Tests for skill level calibration — local JSON persistence."""

import json

from gladekit_mcp.skill import _MIN_MESSAGES, load_skill_level


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
