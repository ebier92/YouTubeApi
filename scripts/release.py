"""Sets up a new major, minor, or patch version bumped release. Commits a version bump 
for the project's version according to the passed "type" argument, adds a new repo tag
based on the bumped project file version, and merges development into main. This causes 
the `lint_check_and_tests.yml` job to run on main which in turn triggers a run of
`release.yml`.
"""

import argparse
import os
import re
from typing import Sequence

TARGET_PROJECT_FILE = "YouTubeApi/YouTubeApi.csproj"
PACKAGE_MANAGER_NAME = "Nuget"
VERSION_REGEX = r"(<Version>)(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?(</Version>)"


def _execute_commands(commands: Sequence[str]):
    for command in commands:
        command_failure = os.system(command)

        if command_failure:
            raise RuntimeError(
                f"ERROR: Git command '{command}' failed to execute successfully."
            )


parser = argparse.ArgumentParser(
    description="Increments the package version and triggers a new release."
)
parser.add_argument(
    "type",
    choices=("major", "minor", "patch"),
    help="The type of version increment to use for the release.",
)
args = parser.parse_args()

_execute_commands(("git checkout development", "git pull"))

with open(TARGET_PROJECT_FILE, "r") as project_file:
    project_file_contents = project_file.read()

version_match = re.search(VERSION_REGEX, project_file_contents)

if not version_match:
    raise RuntimeError(
        f"Unable to locate the current version in {TARGET_PROJECT_FILE}."
    )

major = int(version_match[2])
minor = int(version_match[3])
patch = int(version_match[4])
current_version = f"{major}.{minor}.{patch}"

if args.type == "major":
    major += 1
    minor = 0
    patch = 0
elif args.type == "minor":
    minor += 1
    patch = 0
else:
    patch += 1

new_version = f"{major}.{minor}.{patch}"
project_file_contents = re.sub(
    VERSION_REGEX, r"\g<1>" + new_version + r"\g<7>", project_file_contents
)

print(
    f"You are about to create a new release. This will result in a package {args.type} "
    f"version bump from {current_version} -> {new_version}. A tag and release will be created "
    f"for version {new_version} and this new version will be published on {PACKAGE_MANAGER_NAME}. "
    "Are you sure you want to proceed? (y/n):"
)
prompt = ""

while prompt.lower() not in ("y", "n"):
    prompt = input()

    if prompt not in ("y", "n"):
        print("Please enter 'y' or 'n' to make a selection.")

if prompt == "n":
    print("Release process cancelled.")
    exit()

with open(TARGET_PROJECT_FILE, "w") as pyproject_file:
    pyproject_file.write(project_file_contents)

_execute_commands(
    (
        f"git add {TARGET_PROJECT_FILE}",
        f'git commit -m "Increment {args.type} version."',
        "git push",
        "git checkout main",
        "git merge development",
        "git push",
        f'git tag -a v{new_version} -m "Release {new_version}"',
        f"git push origin v{new_version}",
        "git checkout development",
        "git merge main",
        "git push",
    )
)
