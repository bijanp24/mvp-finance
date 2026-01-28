"""
N-Queens Problem Solver

Place N queens on an N×N chessboard so that no two queens attack each other
(no shared row, column, or diagonal).

A queen attacks along:
  - same column c
  - main diagonal (r - c)
  - anti-diagonal (r + c)

Approach: backtracking — place one queen per row, try columns that aren't
threatened, recurse to the next row, and undo (backtrack) when stuck.

Complexity: worst-case is exponential (roughly O(N!)), but diagonal pruning
makes it workable for moderate N (up to ~12–15 in Python).
"""


def solve_n_queens(n: int):
    """Return all solutions as lists of strings, where 'Q' marks a queen and '.' an empty cell."""
    cols, diag1, diag2 = set(), set(), set()
    board = [-1] * n  # board[row] = col
    solutions = []

    def backtrack(r: int):
        if r == n:
            sol = []
            for row in range(n):
                line = ['.'] * n
                line[board[row]] = 'Q'
                sol.append(''.join(line))
            solutions.append(sol)
            return

        for c in range(n):
            if c in cols or (r - c) in diag1 or (r + c) in diag2:
                continue

            board[r] = c
            cols.add(c)
            diag1.add(r - c)
            diag2.add(r + c)

            backtrack(r + 1)

            cols.remove(c)
            diag1.remove(r - c)
            diag2.remove(r + c)
            board[r] = -1

    backtrack(0)
    return solutions


def count_n_queens(n: int) -> int:
    """Return only the count of solutions (faster than storing boards)."""
    cols, diag1, diag2 = set(), set(), set()
    count = 0

    def backtrack(r: int):
        nonlocal count
        if r == n:
            count += 1
            return

        for c in range(n):
            if c in cols or (r - c) in diag1 or (r + c) in diag2:
                continue

            cols.add(c)
            diag1.add(r - c)
            diag2.add(r + c)

            backtrack(r + 1)

            cols.remove(c)
            diag1.remove(r - c)
            diag2.remove(r + c)

    backtrack(0)
    return count


def solve_n_queens_one(n: int):
    """Return a single solution (or None if no solution exists)."""
    cols, diag1, diag2 = set(), set(), set()
    board = [-1] * n

    def backtrack(r: int) -> bool:
        if r == n:
            return True

        for c in range(n):
            if c in cols or (r - c) in diag1 or (r + c) in diag2:
                continue

            board[r] = c
            cols.add(c)
            diag1.add(r - c)
            diag2.add(r + c)

            if backtrack(r + 1):
                return True

            cols.remove(c)
            diag1.remove(r - c)
            diag2.remove(r + c)
            board[r] = -1

        return False

    if backtrack(0):
        sol = []
        for row in range(n):
            line = ['.'] * n
            line[board[row]] = 'Q'
            sol.append(''.join(line))
        return sol
    return None


if __name__ == '__main__':
    n = 8
    print(f"=== N-Queens (N={n}) ===\n")

    # Count solutions
    total = count_n_queens(n)
    print(f"Total solutions: {total}\n")

    # Show one solution
    one = solve_n_queens_one(n)
    if one:
        print("One solution:")
        for row in one:
            print(f"  {row}")
        print()

    # Show all solutions for a smaller board
    small_n = 4
    sols = solve_n_queens(small_n)
    print(f"=== All {len(sols)} solutions for N={small_n} ===")
    for i, s in enumerate(sols, 1):
        print(f"\nSolution {i}:")
        for row in s:
            print(f"  {row}")
