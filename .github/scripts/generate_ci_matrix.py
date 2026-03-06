#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


def sanitize_key(value: str) -> str:
    return re.sub(r"[^A-Za-z0-9_.-]", "_", value)


def detect_service_type(service_path: Path) -> tuple[str, str, str]:
    has_go = (service_path / "go.mod").exists()
    solutions = sorted(service_path.rglob("*.sln"))
    projects = sorted(service_path.rglob("*.csproj"))
    has_dotnet = bool(solutions or projects)

    if has_go and has_dotnet:
        raise ValueError(
            f"Service at '{service_path}' contains both Go and .NET markers; split into separate service entries."
        )

    if has_go:
        return "go", "", ""

    if has_dotnet:
        solution = str(solutions[0].as_posix()) if solutions else ""
        project = str(projects[0].as_posix()) if projects else ""
        return "dotnet", solution, project

    raise ValueError(
        f"Service at '{service_path}' is neither Go (go.mod) nor .NET (*.sln/*.csproj)."
    )


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--config", required=True)
    parser.add_argument("--repo-root", required=True)
    parser.add_argument("--repo-name", default="repo")
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    config_path = (repo_root / args.config).resolve()

    with config_path.open("r", encoding="utf-8") as handle:
        config = json.load(handle)

    service_entries = config.get("services", [])
    if not service_entries:
        raise ValueError("No services configured in .github/ci/services.json")

    matrix = []
    for entry in service_entries:
        name = entry.get("name")
        path = entry.get("path")

        if not name or not path:
            raise ValueError("Each service entry must have 'name' and 'path'.")

        full_path = (repo_root / path).resolve()
        if not full_path.exists() or not full_path.is_dir():
            raise ValueError(f"Configured service path does not exist or is not a directory: {path}")

        service_type, solution, project = detect_service_type(full_path)
        sonar_project_key = sanitize_key(f"{args.repo_name}_{name}")

        matrix.append(
            {
                "name": name,
                "path": path,
                "type": service_type,
                "solution": solution,
                "project": project,
                "sonarProjectKey": sonar_project_key,
            }
        )

    payload = {"include": matrix}
    print(f"matrix={json.dumps(payload)}")


if __name__ == "__main__":
    main()
