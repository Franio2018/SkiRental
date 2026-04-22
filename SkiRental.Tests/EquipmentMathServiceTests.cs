using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SkiRental.Domain.Enums;
using SkiRental.Domain.Models;
using SkiRental.Logic.Interfaces;
using SkiRental.Logic.Services;
using Xunit;

namespace SkiRental.Tests;

/// <summary>
/// Testy jednostkowe algorytmu doboru sprzętu (EquipmentMathService).
/// Sprawdzają zgodność z normami FIS dla stylów Klasycznego i Łyżwowego
/// oraz poprawność korekt BMI.
/// </summary>
public class EquipmentMathServiceTests
{
    private readonly EquipmentMathService _sut;
    private readonly Mock<IEquipmentRepository> _repoMock;

    // ── Test fixture ─────────────────────────────────────────────────────────
    public EquipmentMathServiceTests()
    {
        _repoMock = new Mock<IEquipmentRepository>();
        _repoMock
            .Setup(r => r.GetAvailableByStyleAsync(
                It.IsAny<SkiStyle>(), It.IsAny<EquipmentCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _sut = new EquipmentMathService(
            _repoMock.Object,
            NullLogger<EquipmentMathService>.Instance);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Styl Klasyczny – długość nart
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(175, 70, SkillLevel.Beginner,     190.0)] // 175+20 = 195 → zaokrąglone do siatki
    [InlineData(175, 70, SkillLevel.Intermediate, 197.5)] // 175+22.5
    [InlineData(175, 70, SkillLevel.Expert,       205.0)] // 175+30
    public void CalculateSkiLength_Classic_NormalBmi_ReturnsExpectedRange(
        int height, decimal weight, SkillLevel skill, double expectedApprox)
    {
        // Arrange
        var p = new EquipmentSelectionParameters
        {
            Height     = height,
            Weight     = weight,
            ShoeSize   = 42,
            Style      = SkiStyle.Classic,
            SkillLevel = skill
        };

        // Act
        var result = _sut.CalculateSkiLength(p);

        // Assert – wynik powinien mieścić się w ±5 cm od expectedApprox
        result.Should().BeApproximately((decimal)expectedApprox, 5m,
            $"dla wzrostu {height} cm i poziomu {skill}");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Styl Łyżwowy – długość nart
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(175, 70, SkillLevel.Beginner, 185.0)]     // 175+10
    [InlineData(175, 70, SkillLevel.Expert,   190.0)]     // 175+15
    [InlineData(180, 75, SkillLevel.Intermediate, 192.5)] // 180+12.5
    public void CalculateSkiLength_Skate_NormalBmi_ReturnsExpectedRange(
        int height, decimal weight, SkillLevel skill, double expectedApprox)
    {
        var p = new EquipmentSelectionParameters
        { Height = height, Weight = weight, ShoeSize = 42, Style = SkiStyle.Skate, SkillLevel = skill };

        var result = _sut.CalculateSkiLength(p);

        result.Should().BeApproximately((decimal)expectedApprox, 5m);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Korekta BMI – długość nart
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CalculateSkiLength_Overweight_AddsCorrection()
    {
        // BMI > 24.9 → narta dłuższa o 5 cm
        var normal = new EquipmentSelectionParameters
        { Height = 175, Weight = 70, Style = SkiStyle.Classic, SkillLevel = SkillLevel.Intermediate };

        var overweight = normal with { Weight = 100 }; // BMI ≈ 32.7

        var normalLen     = _sut.CalculateSkiLength(normal);
        var overweightLen = _sut.CalculateSkiLength(overweight);

        overweightLen.Should().BeGreaterThan(normalLen,
            "nadwaga powinna dodać długości (sztywność)");

        (overweightLen - normalLen).Should().Be(5m, "korekta BMI wynosi dokładnie 5 cm");
    }

    [Fact]
    public void CalculateSkiLength_Underweight_SubtractsCorrection()
    {
        // BMI < 18.5 → narta krótsza o 5 cm
        var normal = new EquipmentSelectionParameters
        { Height = 175, Weight = 70, Style = SkiStyle.Classic, SkillLevel = SkillLevel.Intermediate };

        var underweight = normal with { Weight = 50 }; // BMI ≈ 16.3

        var normalLen      = _sut.CalculateSkiLength(normal);
        var underweightLen = _sut.CalculateSkiLength(underweight);

        underweightLen.Should().BeLessThan(normalLen,
            "niedowaga powinna skrócić nartę");

        (normalLen - underweightLen).Should().Be(5m, "korekta BMI wynosi dokładnie -5 cm");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Kijki
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(175, SkiStyle.Classic, 148.75)] // 175 * 0.85 = 148.75 → zaokrąglone
    [InlineData(175, SkiStyle.Skate,   157.5)]  // 175 * 0.90 = 157.5
    [InlineData(180, SkiStyle.Classic, 152.5)]
    [InlineData(180, SkiStyle.Skate,   162.5)]
    public void CalculatePoleLength_ReturnsCorrectValue(
        int height, SkiStyle style, double expectedApprox)
    {
        var p = new EquipmentSelectionParameters
        { Height = height, Weight = 70, ShoeSize = 42, Style = style, SkillLevel = SkillLevel.Intermediate };

        var result = _sut.CalculatePoleLengthCm(p);

        result.Should().BeApproximately((decimal)expectedApprox, 2.5m,
            $"kijki {style}: {height}cm × {(style == SkiStyle.Classic ? 0.85m : 0.90m)}");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Sztywność (Flex Index)
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(SkillLevel.Beginner,     70, 175, 42)]  // normalny BMI
    [InlineData(SkillLevel.Expert,       70, 175, 72)]
    [InlineData(SkillLevel.Intermediate, 100, 175, 57)] // nadwaga +5 flex
    [InlineData(SkillLevel.Advanced,     50, 175, 57)]  // niedowaga -5 flex
    public void CalculateStiffness_ReturnsExpectedRange(
        SkillLevel skill, decimal weight, int height, int expectedFlex)
    {
        var p = new EquipmentSelectionParameters
        { Height = height, Weight = weight, ShoeSize = 42, Style = SkiStyle.Classic, SkillLevel = skill };

        var result = _sut.CalculateStiffness(p);

        result.Should().BeInRange(expectedFlex - 3, expectedFlex + 3,
            $"Flex dla {skill}, waga {weight} kg");
    }

    [Fact]
    public void CalculateStiffness_AlwaysWithinBounds()
    {
        // Sprawdź że Flex Index zawsze mieści się w 30–90
        foreach (var skill in Enum.GetValues<SkillLevel>())
        {
            foreach (var weight in new[] { 40m, 70m, 130m })
            {
                var p = new EquipmentSelectionParameters
                { Height = 175, Weight = weight, ShoeSize = 42,
                  Style = SkiStyle.Classic, SkillLevel = skill };

                var flex = _sut.CalculateStiffness(p);
                flex.Should().BeInRange(30, 90, $"Flex musi być w zakresie 30–90 dla {skill}/{weight}kg");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // RecommendAsync – integracja z repo
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RecommendAsync_ReturnsRecommendation_WithAlgorithmExplanation()
    {
        var p = new EquipmentSelectionParameters
        {
            Height     = 175,
            Weight     = 75,
            ShoeSize   = 42,
            Style      = SkiStyle.Classic,
            SkillLevel = SkillLevel.Intermediate
        };

        var result = await _sut.RecommendAsync(p);

        result.Should().NotBeNull();
        result.AlgorithmExplanation.Should().NotBeNullOrEmpty();
        result.RecommendedSkiLength.Should().BeGreaterThan(0);
        result.RecommendedPoleLength.Should().BeGreaterThan(0);
        result.Style.Should().Be(SkiStyle.Classic);
        result.BmiCorrectionApplied.Should().BeFalse("BMI 24.5 jest w normie");
    }

    [Fact]
    public async Task RecommendAsync_WithOverweight_SetsBmiCorrectionFlag()
    {
        var p = new EquipmentSelectionParameters
        {
            Height = 175, Weight = 110, ShoeSize = 44,
            Style = SkiStyle.Skate, SkillLevel = SkillLevel.Advanced
        };

        var result = await _sut.RecommendAsync(p);

        result.BmiCorrectionApplied.Should().BeTrue();
        result.BmiCorrectionNote.Should().Contain("nadwaga");
    }
}
