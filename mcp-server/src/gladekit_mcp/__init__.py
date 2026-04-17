"""GladeKit MCP Server — Unity game development tools for AI clients."""

from importlib.metadata import PackageNotFoundError, version

try:
    __version__ = version("gladekit-mcp")
except PackageNotFoundError:
    __version__ = "0.0.0-dev"


def main() -> None:
    """Entry point for the gladekit-mcp / gladekit CLI command."""
    import argparse
    import asyncio
    import sys

    from .bridge import DEFAULT_BRIDGE_URL

    parser = argparse.ArgumentParser(
        prog="gladekit",
        description="GladeKit — AI tools for Unity game development",
    )
    parser.add_argument(
        "--bridge-url",
        default=DEFAULT_BRIDGE_URL,
        help=f"Unity bridge URL (default: {DEFAULT_BRIDGE_URL})",
    )
    subparsers = parser.add_subparsers(dest="command")

    # doctor
    doctor_p = subparsers.add_parser("doctor", help="Diagnose the GladeKit stack")
    doctor_p.add_argument("--json", action="store_true", dest="output_json", help="Output results as JSON")

    # init
    init_p = subparsers.add_parser("init", help="Scaffold GLADE.md from the live Unity project")
    init_p.add_argument("--force", action="store_true", help="Overwrite existing GLADE.md")
    init_p.add_argument("--dry-run", action="store_true", dest="dry_run", help="Preview GLADE.md without writing")

    # version
    subparsers.add_parser("version", help="Show version and check for updates")

    args = parser.parse_args()

    if args.command == "doctor":
        from .cli import run_doctor
        sys.exit(run_doctor(bridge_url=args.bridge_url, output_json=args.output_json))

    elif args.command == "init":
        from .cli import run_init
        sys.exit(run_init(bridge_url=args.bridge_url, force=args.force, dry_run=args.dry_run))

    elif args.command == "version":
        from .cli import run_version
        sys.exit(run_version())

    else:
        # No subcommand → run the MCP server (existing behavior)
        from .server import run_server
        asyncio.run(run_server())
