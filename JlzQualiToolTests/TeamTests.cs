using JlzQualiTool;
using System;
using Xunit;

namespace JlzQualiToolTests
{
    public class TeamTests
    {
        [Fact]
        public void AddOpponent__opponents_can_be_added()
        {
            // Arrange
            var team = new Team("Power Wave");

            var opponent1 = new Team("Ad Astra");
            var opponent2 = new Team("Ad Astra");
            var opponent3 = new Team("Einhorn");
            var opponent4 = new Team("Indians");

            // Act
            team.AddOpponent(opponent1);
            team.AddOpponent(opponent2);
            team.AddOpponent(opponent3);

            // Assert
            Assert.Equal(3, team.Opponents.Count);
            Assert.True(team.HasPlayed(opponent1));
            Assert.True(team.HasPlayed(opponent2));
            Assert.True(team.HasPlayed(opponent3));
            Assert.False(team.HasPlayed(opponent4));
        }

        [Fact]
        public void AddOpponent__throws_if_opponent_already_exists()
        {
            // Arrange
            var team = new Team("Power Wave");

            var opponent1 = new Team("Ad Astra");

            // Act
            team.AddOpponent(opponent1);

            Action action = () => team.AddOpponent(opponent1);

            // Assert
            Assert.Throws<InvalidOperationException>(action);
        }
    }
}