
import re

def resolve_merge_conflicts(file_path):
    current_lines = []
    incoming_lines = []
    
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    in_conflict_block = False
    in_current_side = False

    for line in lines:
        if line.startswith('<<<<<<<'):
            in_conflict_block = True
            in_current_side = True
        elif line.startswith('======='):
            in_current_side = False
        elif line.startswith('>>>>>>>'):
            in_conflict_block = False
            in_current_side = False # Reset for safety
        else:
            if not in_conflict_block:
                current_lines.append(line)
                incoming_lines.append(line)
            elif in_current_side:
                current_lines.append(line)
            else: # incoming side
                incoming_lines.append(line)

    # Write the resolved files
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(current_lines)

    new_file_path = file_path + '-new'
    with open(new_file_path, 'w', encoding='utf-8') as f:
        f.writelines(incoming_lines)

    print(f"'{file_path}' has been overwritten with the current changes.")
    print(f"'{new_file_path}' has been created with the incoming changes.")

if __name__ == '__main__':
    resolve_merge_conflicts('Dungeon.unity')
