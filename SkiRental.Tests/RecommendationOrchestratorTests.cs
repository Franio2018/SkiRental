using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SkiRental.Domain.Enums;
using SkiRental.Domain.Models;
using SkiRental.Logic.Interfaces;
using SkiRental.Logic.Services;
using Xunit;

namespace SkiRental.Tests;

public class RecommendationOrchestratorTests
{
    private readonly Mock<IEquipmentRepository> _repo = new();
    private readonly RecommendationOrchestrator _sut;

    private static readonly EquipmentSelectionParameters DefaultParams = new()
    {
        Height = 175,
        Weight = 70m,
        ShoeSize = 42,
        Style = SkiStyle.Classic,
        SkillLevel = SkillLevel.Intermediate,
    };

    public RecommendationOrchestratorTests()
    {
        var math = new EquipmentMathService(_repo.Object, NullLogger<EquipmentMathService>.Instance);
        _sut = new RecommendationOrchestrator(
            math,
            _repo.Object,
            NullLogger<RecommendationOrchestrator>.Instance);
    }

    [Fact]
    public async Task RecommendAsync_AllCategoriesMatch_ReturnsFullStatus()
    {
        SetupInventory(ClassicInventoryMatching175cm());

        var result = await _sut.RecommendAsync(DefaultParams);

        result.Status.Should().Be(RecommendationStatus.Full);
        result.Selection.Should().HaveCount(3);
        result.NoMatchReasons.Should().BeEmpty();
        result.MatchStatus.Values.Should().OnlyContain(m => m == MatchStatus.ExactMatch);
        result.TotalDailyRate.Should().BeGreaterThan(0);
        result.AlgorithmExplanation.Should().Contain("Classic");
    }

    [Fact]
    public async Task RecommendAsync_SkisOutOfTolerance_ReturnsNoMatch()
    {
        // Ideal classic ski ~197.5cm; only 220cm skis available (outside ±10cm)
        SetupInventory(
            skis: [MakeEquipment(EquipmentCategory.Skis, 220m, SkiStyle.Classic, stiffness: 60)],
            poles: [MakeEquipment(EquipmentCategory.Poles, 147.5m, SkiStyle.Classic)],
            boots: [MakeEquipment(EquipmentCategory.Boots, 42m, null)]);

        var result = await _sut.RecommendAsync(DefaultParams);

        result.Status.Should().Be(RecommendationStatus.NoMatch);
        result.NoMatchReasons.Should().Contain(r => r.Category == EquipmentCategory.Skis);
        result.Selection.Should().BeEmpty();
    }

    [Fact]
    public async Task RecommendAsync_SkisInFallbackBand_ReturnsPartialWithFallbackNote()
    {
        // Ideal ~197.5; 203cm is within max ±10 but outside ideal ±5
        SetupInventory(
            skis: [MakeEquipment(EquipmentCategory.Skis, 203m, SkiStyle.Classic, stiffness: 55)],
            poles: [MakeEquipment(EquipmentCategory.Poles, 147.5m, SkiStyle.Classic)],
            boots: [MakeEquipment(EquipmentCategory.Boots, 42m, null)]);

        var result = await _sut.RecommendAsync(DefaultParams);

        result.Status.Should().Be(RecommendationStatus.Partial);
        result.MatchStatus[EquipmentCategory.Skis].Should().Be(MatchStatus.FallbackMatch);
        result.FallbackNotes.Should().ContainKey(EquipmentCategory.Skis);
        result.Selection.Should().ContainKey(EquipmentCategory.Skis);
    }

    [Fact]
    public async Task RecommendAsync_EmptyInventory_ReturnsNoMatchForAllCategories()
    {
        SetupInventory(skis: [], poles: [], boots: []);

        var result = await _sut.RecommendAsync(DefaultParams);

        result.Status.Should().Be(RecommendationStatus.NoMatch);
        result.NoMatchReasons.Should().HaveCount(3);
        result.NoMatchReasons.Should().OnlyContain(r =>
            r.AvailableSizes == "None available");
    }

    private void SetupInventory(
        IReadOnlyList<Equipment>? skis = null,
        IReadOnlyList<Equipment>? poles = null,
        IReadOnlyList<Equipment>? boots = null)
    {
        skis ??= ClassicInventoryMatching175cm().Where(e => e.Category == EquipmentCategory.Skis).ToList();
        poles ??= ClassicInventoryMatching175cm().Where(e => e.Category == EquipmentCategory.Poles).ToList();
        boots ??= ClassicInventoryMatching175cm().Where(e => e.Category == EquipmentCategory.Boots).ToList();

        _repo.Setup(r => r.GetAvailableByStyleAsync(SkiStyle.Classic, EquipmentCategory.Skis, It.IsAny<CancellationToken>()))
            .ReturnsAsync(skis);
        _repo.Setup(r => r.GetAvailableByStyleAsync(SkiStyle.Classic, EquipmentCategory.Poles, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poles);
        _repo.Setup(r => r.GetAvailableByStyleAsync(SkiStyle.Classic, EquipmentCategory.Boots, It.IsAny<CancellationToken>()))
            .ReturnsAsync(boots);
    }

    private static List<Equipment> ClassicInventoryMatching175cm() =>
    [
        MakeEquipment(EquipmentCategory.Skis, 197.5m, SkiStyle.Classic, stiffness: 52),
        MakeEquipment(EquipmentCategory.Poles, 150m, SkiStyle.Classic), // ideal pole = 175 × 0.85 → 150 cm
        MakeEquipment(EquipmentCategory.Boots, 42m, null),
    ];

    private static Equipment MakeEquipment(
        EquipmentCategory category,
        decimal size,
        SkiStyle? style,
        int? stiffness = null,
        int id = 0)
    {
        return new Equipment
        {
            Id = id != 0 ? id : Random.Shared.Next(1, 10000),
            SerialNumber = $"MOCK-{category}-{size}",
            Brand = "Mock",
            Model = "Mock",
            Category = category,
            Size = size,
            Stiffness = stiffness,
            Status = EquipmentStatus.Available,
            CompatibleStyle = style,
            YearOfProduction = 2024,
            DailyRate = 50m,
        };
    }
}
