import json
from os import listdir
from os.path import isfile, join
import hashlib

config = {
    "working_directory": '/path/to/spectero/daemon_and_cli/directory',
    "daemon_directory": "./daemon",
    "cli_directory": "./cli",
    "merge_directory": "./libs"
}

daemon_definitions = {}
compared_definitions = {}
final_definitions = {}


def digest(filename):
    sha256 = hashlib.sha256()
    with open(filename, 'rb') as f:
        for block in iter(lambda: f.read(65535), b''):
            sha256.update(block)

    return sha256.hexdigest()


daemon_files = [
    f for f in listdir(join(config["working_directory"], config["daemon_directory"]))
    if isfile(join(join(config["working_directory"], config["daemon_directory"]), f))
]
cli_files = [
    f for f in listdir(join(config["working_directory"], config["cli_directory"]))
    if isfile(join(join(config["working_directory"], config["cli_directory"]), f))
]

# Find the DLL Files in the daemon.
for file in daemon_files:
    if file.endswith(".dll"):
        abs_path = join(config["working_directory"], config["daemon_directory"], file)
        daemon_definitions[file] = {
            "path": abs_path,
            "daemon_digest": digest(abs_path)
        }

# Compare the CLI libraries to the ones found in the daemon.
for file in cli_files:
    if file.endswith(".dll"):
        abs_path = join(config["working_directory"], config["cli_directory"], file)
        if file in daemon_definitions:
            compared_definitions[file] = {
                "path": daemon_definitions[file]["path"],
                "daemon_digest": daemon_definitions[file]["daemon_digest"],
                "cli_digest": digest(abs_path)
            }

# Make the final comparison and see what libraries daemon and the cli has.
for definition in compared_definitions:
    if compared_definitions[definition]["daemon_digest"] == compared_definitions[definition]["cli_digest"]:
        final_definitions[definition] = compared_definitions[definition]


print(json.dumps(final_definitions))
