"""
Sudoku Solver (Backtracking)

Solve a 9x9 Sudoku puzzle by placing digits 1-9 such that each row, column,
and 3x3 box contains each digit exactly once.

Approach: backtracking with constraint sets.
  - For each empty cell, try digits that don't conflict with its row, column,
    and 3x3 box. Recurse to the next empty cell. Undo (backtrack) when stuck.
  - Maintain row/col/box "used digit" sets so conflict checks are O(1).

Board representation: 9x9 list of lists of ints; 0 means empty.

Same place/recurse/undo pattern as the N-Queens solver — just with a
different constraint check.
"""

from copy import deepcopy

N = 9
BOX = 3


def _box_index(r: int, c: int) -> int:
    return (r // BOX) * BOX + (c // BOX)


def _build_constraints(board):
    """Initialize row/col/box used-digit sets from the given board."""
    rows = [set() for _ in range(N)]
    cols = [set() for _ in range(N)]
    boxes = [set() for _ in range(N)]
    for r in range(N):
        for c in range(N):
            v = board[r][c]
            if v != 0:
                rows[r].add(v)
                cols[c].add(v)
                boxes[_box_index(r, c)].add(v)
    return rows, cols, boxes


def _find_empty(board):
    """Return (r, c) of the next empty cell, or None if the board is full."""
    for r in range(N):
        for c in range(N):
            if board[r][c] == 0:
                return r, c
    return None


def solve(board):
    """Solve the puzzle in place. Return True if a solution is found, else False.

    Mutates `board`. Pass a copy if you want to preserve the input.
    """
    rows, cols, boxes = _build_constraints(board)

    def backtrack() -> bool:
        empty = _find_empty(board)
        if empty is None:
            return True
        r, c = empty
        b = _box_index(r, c)

        for v in range(1, 10):
            if v in rows[r] or v in cols[c] or v in boxes[b]:
                continue

            board[r][c] = v
            rows[r].add(v)
            cols[c].add(v)
            boxes[b].add(v)

            if backtrack():
                return True

            board[r][c] = 0
            rows[r].remove(v)
            cols[c].remove(v)
            boxes[b].remove(v)

        return False

    return backtrack()


def solve_one(board):
    """Return a solved copy of the board, or None if unsolvable."""
    work = deepcopy(board)
    return work if solve(work) else None


def count_solutions(board, limit: int = None) -> int:
    """Count the number of solutions. Optionally stop early after `limit` are found.

    Useful for verifying a puzzle has a unique solution: pass limit=2 and
    check the result is exactly 1.
    """
    work = deepcopy(board)
    rows, cols, boxes = _build_constraints(work)
    count = 0

    def backtrack():
        nonlocal count
        if limit is not None and count >= limit:
            return
        empty = _find_empty(work)
        if empty is None:
            count += 1
            return
        r, c = empty
        b = _box_index(r, c)

        for v in range(1, 10):
            if v in rows[r] or v in cols[c] or v in boxes[b]:
                continue

            work[r][c] = v
            rows[r].add(v)
            cols[c].add(v)
            boxes[b].add(v)

            backtrack()

            work[r][c] = 0
            rows[r].remove(v)
            cols[c].remove(v)
            boxes[b].remove(v)

            if limit is not None and count >= limit:
                return

    backtrack()
    return count


def format_board(board) -> str:
    """Pretty-print a Sudoku board with separators between 3x3 boxes."""
    lines = []
    for r in range(N):
        if r > 0 and r % BOX == 0:
            lines.append("------+-------+------")
        row_parts = []
        for c in range(N):
            if c > 0 and c % BOX == 0:
                row_parts.append("|")
            v = board[r][c]
            row_parts.append(str(v) if v != 0 else ".")
        lines.append(" ".join(row_parts))
    return "\n".join(lines)


if __name__ == '__main__':
    puzzle = [
        [5, 3, 0, 0, 7, 0, 0, 0, 0],
        [6, 0, 0, 1, 9, 5, 0, 0, 0],
        [0, 9, 8, 0, 0, 0, 0, 6, 0],
        [8, 0, 0, 0, 6, 0, 0, 0, 3],
        [4, 0, 0, 8, 0, 3, 0, 0, 1],
        [7, 0, 0, 0, 2, 0, 0, 0, 6],
        [0, 6, 0, 0, 0, 0, 2, 8, 0],
        [0, 0, 0, 4, 1, 9, 0, 0, 5],
        [0, 0, 0, 0, 8, 0, 0, 7, 9],
    ]

    print("Puzzle:")
    print(format_board(puzzle))
    print()

    solved = solve_one(puzzle)
    if solved:
        print("Solution:")
        print(format_board(solved))
        print()

    n = count_solutions(puzzle, limit=2)
    print(f"Solutions found (capped at 2): {n}  -> {'unique' if n == 1 else 'not unique / unsolvable'}")
