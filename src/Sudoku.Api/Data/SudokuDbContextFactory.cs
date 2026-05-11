using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sudoku.Api.Data;

public sealed class SudokuDbContextFactory : IDesignTimeDbContextFactory<SudokuDbContext>
{
    public SudokuDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SudokuDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=sudokudb;Username=postgres;Password=postgres");

        return new SudokuDbContext(optionsBuilder.Options);
    }
}
