using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sudoku.Api.Contracts;
using Sudoku.Api.GameEngine;
using Xunit;

namespace Sudoku.Api.Tests;

public sealed class SudokuEngineTests
{
    private static readonly int[] KnownPuzzle =
    [
        5, 3, 0, 0, 7, 0, 0, 0, 0,
        6, 0, 0, 1, 9, 5, 0, 0, 0,
        0, 9, 8, 0, 0, 0, 0, 6, 0,
        8, 0, 0, 0, 6, 0, 0, 0, 3,
        4, 0, 0, 8, 0, 3, 0, 0, 1,
        7, 0, 0, 0, 2, 0, 0, 0, 6,
        0, 6, 0, 0, 0, 0, 2, 8, 0,
        0, 0, 0, 4, 1, 9, 0, 0, 5,
        0, 0, 0, 0, 8, 0, 0, 7, 9
    ];

    private static readonly int[] KnownSolution =
    [
        5, 3, 4, 6, 7, 8, 9, 1, 2,
        6, 7, 2, 1, 9, 5, 3, 4, 8,
        1, 9, 8, 3, 4, 2, 5, 6, 7,
        8, 5, 9, 7, 6, 1, 4, 2, 3,
        4, 2, 6, 8, 5, 3, 7, 9, 1,
        7, 1, 3, 9, 2, 4, 8, 5, 6,
        9, 6, 1, 5, 3, 7, 2, 8, 4,
        2, 8, 7, 4, 1, 9, 6, 3, 5,
        3, 4, 5, 2, 8, 6, 1, 7, 9
    ];

    [Fact]
    public void SolverSolvesKnownPuzzleWithUniqueSolution()
    {
        var solver = new SudokuSolver();

        var solved = solver.TrySolve(KnownPuzzle, out var solution);

        solved.Should().BeTrue();
        solution.Should().Equal(KnownSolution);
        solver.CountSolutions(KnownPuzzle, limit: 2).Should().Be(1);
        solver.IsValidSolution(solution).Should().BeTrue();
    }

    [Fact]
    public void SolverRejectsConflictingGivens()
    {
        var puzzle = KnownPuzzle.ToArray();
        puzzle[2] = 5;
        var solver = new SudokuSolver();

        solver.CountSolutions(puzzle, limit: 2).Should().Be(0);
        solver.TrySolve(puzzle, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(Difficulty.Easy)]
    [InlineData(Difficulty.Medium)]
    [InlineData(Difficulty.Hard)]
    public void GeneratorCreatesPuzzleWithUniqueSolutionInDifficultyBand(Difficulty difficulty)
    {
        var solver = new SudokuSolver();
        var generator = new SudokuGenerator(solver);

        var puzzle = generator.Generate(difficulty, CancellationToken.None);

        var options = DifficultyOptions.For(difficulty);
        puzzle.Puzzle.Count(value => value != 0).Should().BeInRange(options.MinimumClues, options.MaximumClues);
        solver.IsValidSolution(puzzle.Solution).Should().BeTrue();
        solver.CountSolutions(puzzle.Puzzle, limit: 2).Should().Be(1);
    }

    [Fact]
    public async Task GenerationServiceRetriesTimedOutAttempts()
    {
        var service = new PuzzleGenerationService(
            new CancelingGenerator(),
            Options.Create(new PuzzleGenerationOptions
            {
                MaximumAttempts = 2,
                AttemptTimeout = TimeSpan.FromMilliseconds(1)
            }),
            NullLogger<PuzzleGenerationService>.Instance);

        await service.Invoking(service => service.GenerateAsync(Difficulty.Easy, CancellationToken.None))
            .Should()
            .ThrowAsync<PuzzleGenerationException>();
    }

    private sealed class CancelingGenerator : ISudokuGenerator
    {
        public GeneratedPuzzle Generate(Difficulty difficulty, CancellationToken cancellationToken)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }
}
