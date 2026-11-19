using Microsoft.EntityFrameworkCore;
using SportsCourtBooking.Domain.Entities;
using SportsCourtBooking.Infrastructure.Data;

namespace SportsCourtBooking.Tests;

public class OwnerCourtRelationshipTests
{
    [Fact]
    public void Court_HasRequiredOwnerForeignKeyWithRestrictedDelete()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=sportscourt;Username=postgres")
            .Options;
        using var context = new AppDbContext(options);

        var court = context.Model.FindEntityType(typeof(Court));
        Assert.NotNull(court);

        var ownerForeignKey = Assert.Single(court!.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(User));

        Assert.Equal(nameof(Court.OwnerId), Assert.Single(ownerForeignKey.Properties).Name);
        Assert.Equal(DeleteBehavior.Restrict, ownerForeignKey.DeleteBehavior);
        Assert.Contains(court.GetIndexes(), index => index.Properties.Any(property => property.Name == nameof(Court.OwnerId)));
    }
}
