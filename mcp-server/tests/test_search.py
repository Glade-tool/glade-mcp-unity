"""Tests for script semantic search — validates graceful fallback behaviour."""

from unittest.mock import AsyncMock, patch

import pytest

from gladekit_mcp.search import is_available, search_scripts

SAMPLE_SCRIPTS = [
    {
        "name": "PlayerController",
        "path": "Assets/Scripts/PlayerController.cs",
        "content": "void Update() {}",
    },
    {
        "name": "EnemyAI",
        "path": "Assets/Scripts/EnemyAI.cs",
        "content": "void Start() {}",
    },
    {
        "name": "GameManager",
        "path": "Assets/Scripts/GameManager.cs",
        "content": "void Awake() {}",
    },
]


@pytest.mark.asyncio
async def test_search_falls_back_without_api_key(monkeypatch):
    """No OPENAI_API_KEY → returns unranked scripts with similarity=None."""
    monkeypatch.delenv("OPENAI_API_KEY", raising=False)
    results = await search_scripts("jump movement", SAMPLE_SCRIPTS, top_n=2)
    assert len(results) == 2
    assert all(r["similarity"] is None for r in results)


@pytest.mark.asyncio
async def test_search_empty_scripts():
    """Empty script list → empty result."""
    results = await search_scripts("anything", [], top_n=5)
    assert results == []


@pytest.mark.asyncio
async def test_search_top_n_respected(monkeypatch):
    """top_n limits the number of results even without semantic search."""
    monkeypatch.delenv("OPENAI_API_KEY", raising=False)
    results = await search_scripts("query", SAMPLE_SCRIPTS, top_n=1)
    assert len(results) == 1


@pytest.mark.asyncio
async def test_search_rate_limit_falls_back(monkeypatch):
    """OpenAI RateLimitError → graceful fallback, not an unhandled exception."""
    monkeypatch.setenv("OPENAI_API_KEY", "sk-fake-key")

    # Only patch if openai is installed
    try:
        import openai
    except ImportError:
        pytest.skip("openai package not installed")

    import httpx

    fake_request = httpx.Request("POST", "https://api.openai.com/v1/embeddings")
    fake_response = httpx.Response(429, request=fake_request)

    with patch("gladekit_mcp.search._embed_batch", new_callable=AsyncMock) as mock_embed:
        mock_embed.side_effect = openai.RateLimitError(
            message="Rate limit exceeded",
            response=fake_response,
            body=None,
        )
        results = await search_scripts("player movement", SAMPLE_SCRIPTS, top_n=3)

    # Should fall back to unranked, not raise
    assert len(results) == 3
    assert all(r["similarity"] is None for r in results)


def test_is_available_without_key(monkeypatch):
    """is_available() returns False when OPENAI_API_KEY is not set."""
    monkeypatch.delenv("OPENAI_API_KEY", raising=False)
    assert not is_available()
