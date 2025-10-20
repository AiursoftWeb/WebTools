using Aiursoft.WebTools.Data;

namespace Aiursoft.WebTools.Tests.Data
{
    [TestClass]
    public class ZoneNumbersTests
    {
        [TestMethod]
        public void Numbers_ShouldContainValidData()
        {
            // Arrange

            // Act
            var numbers = ZoneNumbers.Numbers;

            // Assert
            Assert.IsNotNull(numbers);
            Assert.IsInstanceOfType(numbers, typeof(Dictionary<string, int>));
            Assert.IsNotEmpty(numbers);
        }

        [TestMethod]
        public void Numbers_ShouldContainValidNumberForExistingCountry()
        {
            // Arrange
            var country = "China";
            var expectedNumber = 86;

            // Act
            var numbers = ZoneNumbers.Numbers;

            // Assert
            Assert.IsTrue(numbers.ContainsKey(country));
            Assert.AreEqual(expectedNumber, numbers[country]);
        }

        [TestMethod]
        public void Numbers_ShouldNotContainNumberForNonExistingCountry()
        {
            // Arrange
            var country = "NonExistingCountry";

            // Act
            var numbers = ZoneNumbers.Numbers;

            // Assert
            Assert.IsFalse(numbers.ContainsKey(country));
        }
    }
}
