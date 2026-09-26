using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Editing;
using Xunit;

namespace OpenUtau.Core {
    public class AutoPitchRandomizerBatchEditTest {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void GeneratesBoundedCurveForEveryPattern(int pattern) {
            var points = AutoPitchRandomizerBatchEdit.GeneratePatternPoints(
                pattern, 500, 15, new Random(1234));

            Assert.Equal(5, points.Count);
            Assert.InRange(Math.Abs(points[0].X), 0, 0.001f);
            Assert.InRange(Math.Abs(points[^1].X - 500), 0, 0.001f);
            Assert.All(points, point => Assert.InRange(point.Y, -15, 15));
            Assert.True(points.Zip(points.Skip(1), (left, right) => left.X < right.X).All(increasing => increasing));
        }

        [Fact]
        public void RandomMixCanSelectAllSixPatterns() {
            var random = new Random(1234);
            var patterns = new HashSet<int>();

            for (int i = 0; i < 1000; i++) {
                patterns.Add(AutoPitchRandomizerBatchEdit.ResolvePattern(
                    AutoPitchRandomizerBatchEdit.RandomPattern, random));
            }

            Assert.Equal(Enumerable.Range(0, AutoPitchRandomizerBatchEdit.PatternCount), patterns.OrderBy(value => value));
        }

        [Fact]
        public void RejectsUnknownPattern() {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                AutoPitchRandomizerBatchEdit.GeneratePatternPoints(
                    AutoPitchRandomizerBatchEdit.PatternCount, 500, 15, new Random(1234)));
        }

        [Fact]
        public void ZeroDurationProducesOneSafePoint() {
            var points = AutoPitchRandomizerBatchEdit.GeneratePatternPoints(
                0, 0, 15, new Random(1234));

            var point = Assert.Single(points);
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);
        }
    }
}
