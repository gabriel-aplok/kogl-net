import os
import time
import sys
import random

def should_ignore(path, root):
    ignore_dirs = {'.git', '.vscode', 'bin', 'obj', 'out', '.idea', '.vs', 'packages',
                   'BenchmarkDotNet.Artifacts', 'website', 'files', '__pycache__', 'samples'}
    ignore_exts = {'.nupkg', '.log', '.user', '.suo', '.DS_Store', '.stackdump'}

    rel = os.path.relpath(path, root).lower()
    name = os.path.basename(path).lower()

    if any(ignored in rel.split(os.sep) for ignored in ignore_dirs):
        return True
    if any(name.endswith(ext) for ext in ignore_exts):
        return True
    return False

def generate_llm_txt():
    root_dir = "."
    output_file = "files/llm.txt"

    if len(sys.argv) > 1 and sys.argv[1] == "rand":
        rand_id = random.randint(100000, 999999)
        output_file = f"files/llm_{rand_id}.txt"

    start_time = time.time()
    file_count = 0
    total_tokens = 0

    with open(output_file, "w", encoding="utf-8") as f:
        f.write("Directory structure:\n")
        for dirpath, dirnames, filenames in os.walk(root_dir):
            dirnames[:] = [d for d in dirnames if not should_ignore(os.path.join(dirpath, d), root_dir)]
            if should_ignore(dirpath, root_dir):
                continue
            level = dirpath.replace(root_dir, "").count(os.sep)
            indent = "    " * level
            basename = os.path.basename(dirpath)
            if basename:
                f.write(f"{indent}└── {basename}/\n")
                subindent = "    " * (level + 1)
                for fname in sorted(f for f in filenames if f.endswith('.cs')):
                    f.write(f"{subindent}└── {fname}\n")

        f.write("\n" + "="*80 + "\n\n")

        for dirpath, dirnames, filenames in os.walk(root_dir):
            dirnames[:] = [d for d in dirnames if not should_ignore(os.path.join(dirpath, d), root_dir)]
            for fname in sorted(f for f in filenames if f.endswith('.cs')):
                filepath = os.path.join(dirpath, fname)
                if should_ignore(filepath, root_dir):
                    continue
                rel_path = os.path.relpath(filepath, root_dir)
                try:
                    with open(filepath, "r", encoding="utf-8") as code:
                        content = code.read()
                    f.write(f"FILE: {rel_path}\n")
                    f.write("="*80 + "\n")
                    f.write(content)
                    f.write("\n\n" + "="*80 + "\n\n")

                    file_count += 1
                    total_tokens += len(content) // 4
                except:
                    pass

    elapsed = time.time() - start_time
    print(f"{output_file} generated!")
    print(f"Files analyzed: {file_count}")
    print(f"Estimated tokens: ~{total_tokens:,}")
    print(f"Time taken: {elapsed:.2f} seconds")

if __name__ == "__main__":
    generate_llm_txt()
