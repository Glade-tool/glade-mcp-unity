"""Console and JSON reporting for MCP eval results."""
from __future__ import annotations

import json
import time
from collections import defaultdict
from pathlib import Path
from typing import Any, Optional, Sequence

from eval.cases import MCPEvalResult

# ANSI codes
_GREEN = "\033[92m"
_RED = "\033[91m"
_YELLOW = "\033[93m"
_CYAN = "\033[96m"
_BOLD = "\033[1m"
_DIM = "\033[2m"
_RESET = "\033[0m"


def _c(text: str, *codes: str) -> str:
    return "".join(codes) + text + _RESET


def _percentile(values: list[float], pct: float) -> float:
    if not values:
        return 0.0
    s = sorted(values)
    k = (len(s) - 1) * pct / 100
    lo = int(k)
    hi = min(lo + 1, len(s) - 1)
    return s[lo] + (k - lo) * (s[hi] - s[lo])


def print_result(r: MCPEvalResult, verbose: bool = False) -> None:
    icon = _c("PASS", _GREEN, _BOLD) if r.passed else _c("FAIL", _RED, _BOLD)
    prompt_preview = (r.prompt[:60] + "...") if len(r.prompt) > 63 else r.prompt

    quality_badge = ""
    if r.quality_score > 0 and r.quality_score < 100:
        q_color = _GREEN if r.quality_score >= 80 else (_YELLOW if r.quality_score >= 60 else _RED)
        quality_badge = f"  {_c(f'Q:{r.quality_score}', q_color)}"

    print(f"  {icon} {_c(f'[{r.case_id}]', _DIM)} {prompt_preview}{quality_badge}")

    if verbose or not r.passed:
        tools_str = (
            _c(", ".join(r.tool_calls_made), _CYAN)
            if r.tool_calls_made
            else _c("(no tools called)", _DIM)
        )
        print(f"       tools: {tools_str}")
        print(f"       {_c(f'{r.duration_seconds:.1f}s', _DIM)} | iter {r.total_iterations}")

        for reason in r.failure_reasons:
            print(f"       {_c('! ' + reason, _YELLOW)}")


def print_summary(results: Sequence[MCPEvalResult]) -> None:
    if not results:
        print("\nNo results to summarize.")
        return

    passed = sum(1 for r in results if r.passed)
    total = len(results)
    pct = int(passed / total * 100)
    color = _GREEN if pct >= 80 else (_YELLOW if pct >= 50 else _RED)

    print()
    print(_c("-" * 62, _DIM))
    print(f"  {_c('Score:', _BOLD)} {_c(f'{passed}/{total}  ({pct}%)', color, _BOLD)}")

    durations = [r.duration_seconds for r in results]
    avg_dur = sum(durations) / len(durations)
    p50 = _percentile(durations, 50)
    p95 = _percentile(durations, 95)
    print(f"  Latency  : avg {avg_dur:.1f}s | p50 {p50:.1f}s | p95 {p95:.1f}s")

    passing = [r for r in results if r.passed]
    if passing:
        avg_q = sum(r.quality_score for r in passing) / len(passing)
        q_color = _GREEN if avg_q >= 80 else (_YELLOW if avg_q >= 60 else _RED)
        print(f"  Quality  : {_c(f'{avg_q:.0f}/100 avg', q_color)} (passing cases)")

    # Per-category breakdown
    category_data: dict[str, list[MCPEvalResult]] = defaultdict(list)
    for r in results:
        if r.category:
            category_data[r.category].append(r)

    if len(category_data) > 1:
        print()
        print(f"  {_c('Per-category:', _BOLD)}")
        for cat, cat_results in sorted(category_data.items()):
            cat_pass = sum(1 for r in cat_results if r.passed)
            pass_color = _GREEN if cat_pass == len(cat_results) else _RED
            print(f"    {cat:<20} {_c(f'{cat_pass}/{len(cat_results)}', pass_color)}")

    # Capability vs regression
    cap = [r for r in results if r.suite_type == "capability"]
    reg = [r for r in results if r.suite_type == "regression"]
    if cap and reg:
        cap_pass = sum(1 for r in cap if r.passed)
        reg_pass = sum(1 for r in reg if r.passed)
        print()
        print(f"  {_c('Capability:', _BOLD)} {cap_pass}/{len(cap)}")
        print(f"  {_c('Regression:', _BOLD)} {reg_pass}/{len(reg)}")
        if reg_pass < len(reg):
            ids = [r.case_id for r in reg if not r.passed]
            print(f"    {_c('REGRESSION FAILURES:', _RED)} {', '.join(ids)}")

    failed = [r for r in results if not r.passed]
    if failed:
        print()
        print(f"  {_c('Failed:', _RED)} {', '.join(r.case_id for r in failed)}")
    print(_c("-" * 62, _DIM))


def save_json(
    results: Sequence[MCPEvalResult],
    output_dir: str = "eval/results",
    metadata: Optional[dict[str, Any]] = None,
) -> str:
    """Persist results as timestamped JSON. Returns file path."""
    Path(output_dir).mkdir(parents=True, exist_ok=True)
    ts = int(time.time())
    path = f"{output_dir}/mcp_eval_{ts}.json"

    data: dict[str, Any] = {
        "timestamp": ts,
        "passed": sum(1 for r in results if r.passed),
        "total": len(results),
        "metadata": metadata or {},
        "results": [
            {
                "case_id": r.case_id,
                "prompt": r.prompt,
                "passed": r.passed,
                "category": r.category,
                "suite_type": r.suite_type,
                "tool_calls_made": r.tool_calls_made,
                "tool_calls_with_args": r.tool_calls_with_args,
                "required_missing": r.required_missing,
                "forbidden_called": r.forbidden_called,
                "any_of_satisfied": r.any_of_satisfied,
                "param_assertion_failures": r.param_assertion_failures,
                "total_iterations": r.total_iterations,
                "duration_seconds": r.duration_seconds,
                "time_to_first_tool_call": r.time_to_first_tool_call,
                "latency_budget_exceeded": r.latency_budget_exceeded,
                "quality_score": r.quality_score,
                "error": r.error,
                "failure_reasons": r.failure_reasons,
            }
            for r in results
        ],
    }

    with open(path, "w") as f:
        json.dump(data, f, indent=2)
    return path
