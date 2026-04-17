"""
GladeKit MCP Eval Harness — CLI entry point.

Usage (from mcp-server/)
------------------------
  # Run core suite against mock bridge:
  python -m eval.run

  # Run all suites:
  python -m eval.run --suite all

  # Run specific cases by tag:
  python -m eval.run --filter scripting

  # Run against live Unity bridge:
  python -m eval.run --live-unity

  # Verbose output + save results:
  python -m eval.run --verbose --save

Exit code: 0 if all cases pass, 1 otherwise.
"""
from __future__ import annotations

import argparse
import asyncio
import sys
from pathlib import Path
from typing import Optional

sys.path.insert(0, str(Path(__file__).parent.parent))
# Also add mcp-server/src to path for gladekit_mcp imports
sys.path.insert(0, str(Path(__file__).parent.parent / "src"))

from eval.cases.core import CORE
from eval.cases.extended import EXTENDED
from eval.cases.negative import NEGATIVE
from eval.harness.client import run_case_direct
from eval.harness.mock_bridge import start_mock_bridge
from eval.harness.reporter import print_result, print_summary, save_json

SUITES = {
    "core": CORE,
    "extended": EXTENDED,
    "negative": NEGATIVE,
    "all": CORE + EXTENDED + NEGATIVE,
}


async def _run_suite(
    cases,
    bridge_url: str,
    verbose: bool,
    concurrency: int,
    filter_str: Optional[str],
    timeout: float,
    latency_budget: Optional[float],
) -> list:
    if filter_str:
        cases = [
            c for c in cases
            if filter_str in c.id or any(filter_str in t for t in c.tags)
        ]

    if not cases:
        print("No cases matched the filter.")
        return []

    print(f"\nGladeKit MCP Eval Harness")
    print(f"  Suite         : {len(cases)} case(s)")
    print(f"  Bridge        : {bridge_url}")
    print(f"  Parallel      : {concurrency}")
    print()

    sem = asyncio.Semaphore(concurrency)

    async def _run_one(case):
        async with sem:
            result = await run_case_direct(
                case, bridge_url, timeout=timeout, latency_budget=latency_budget
            )
            print_result(result, verbose=verbose)
            return result

    tasks = [asyncio.create_task(_run_one(c)) for c in cases]
    return list(await asyncio.gather(*tasks))


def main() -> None:
    parser = argparse.ArgumentParser(
        description="GladeKit MCP Eval Harness",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    parser.add_argument("--suite", default="core", choices=list(SUITES))
    parser.add_argument("--filter", metavar="SUBSTR")
    parser.add_argument("--concurrency", "-c", type=int, default=1)
    parser.add_argument("--timeout", type=float, default=60)
    parser.add_argument("--latency-budget", type=float, default=None)
    parser.add_argument("--save", action="store_true")
    parser.add_argument("--verbose", "-v", action="store_true")
    parser.add_argument(
        "--live-unity",
        action="store_true",
        help="Use real Unity bridge instead of mock (requires Unity open)",
    )
    parser.add_argument(
        "--unity-url",
        default="http://localhost:8765",
        help="Unity bridge URL (with --live-unity)",
    )
    args = parser.parse_args()

    # Start mock bridge or use live Unity
    mock_server = None
    if args.live_unity:
        bridge_url = args.unity_url
        print(f"\nUsing live Unity bridge at {bridge_url}")

        # Pre-flight check
        import httpx
        try:
            resp = httpx.get(f"{bridge_url}/api/health", timeout=5)
            health = resp.json()
            print(f"  Unity {health.get('unityVersion', '?')}, project: {health.get('projectName', '?')}")
        except Exception as e:
            print(f"  ERROR: Unity bridge not reachable at {bridge_url}: {e}")
            sys.exit(1)
    else:
        mock_server = start_mock_bridge()
        bridge_url = f"http://127.0.0.1:{mock_server.server_address[1]}"
        print(f"\nStarted mock Unity bridge on {bridge_url}")

    try:
        suite = SUITES[args.suite]
        results = asyncio.run(
            _run_suite(
                cases=suite,
                bridge_url=bridge_url,
                verbose=args.verbose,
                concurrency=args.concurrency,
                filter_str=args.filter,
                timeout=args.timeout,
                latency_budget=args.latency_budget,
            )
        )

        print_summary(results)

        if args.save and results:
            metadata = {
                "suite": args.suite,
                "bridge_url": bridge_url,
                "live_unity": args.live_unity,
                "concurrency": args.concurrency,
            }
            path = save_json(results, metadata=metadata)
            print(f"\n  Results saved -> {path}")

        failed = sum(1 for r in results if not r.passed)
        sys.exit(1 if failed else 0)

    finally:
        if mock_server:
            mock_server.shutdown()


if __name__ == "__main__":
    main()
