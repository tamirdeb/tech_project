using Xunit;
using EnforcementTool;
using System.Security.Cryptography;
using System.Text;

namespace EnforcementTool.Tests
{
    public class LogicTests
    {
        // =============================================================
        // VerifyHash Tests
        // =============================================================

        [Fact]
        public void VerifyHash_CorrectHash_ReturnsTrue()
        {
            // Arrange
            // We need to calculate the hash of "123" to match the OverrideHash in the original code
            // (a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3)
            string password = "123";
            string hash = "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3";

            // Act
            bool result = EnforcementLogic.VerifyHash(password, hash);

            // Assert
            Assert.True(result, "Hash verification failed for correct password.");
        }

        [Fact]
        public void VerifyHash_IncorrectPassword_ReturnsFalse()
        {
            // Arrange
            string password = "wrongpassword";
            string hash = "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3";

            // Act
            bool result = EnforcementLogic.VerifyHash(password, hash);

            // Assert
            Assert.False(result, "Hash verification succeeded for wrong password.");
        }

        [Theory]
        [InlineData("test", "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08")] // sha256 of "test"
        public void VerifyHash_DynamicTest_ReturnsTrue(string input, string expectedHash)
        {
            bool result = EnforcementLogic.VerifyHash(input, expectedHash);
            Assert.True(result);
        }

        // =============================================================
        // ContainsBannedKeyword Tests
        // =============================================================

        [Theory]
        [InlineData("x.com")]
        [InlineData("twitter")]
        [InlineData("reddit")]
        [InlineData("telegram")]
        [InlineData("discord")]
        [InlineData("This is a url with x.com inside")]
        [InlineData("SEARCHING FOR HENTAI")]
        [InlineData("downloading bittorrent client")]
        public void ContainsBannedKeyword_BannedWords_ReturnsTrue(string input)
        {
            bool result = EnforcementLogic.ContainsBannedKeyword(input);
            Assert.True(result, $"Failed to detect banned keyword in: {input}");
        }

        [Theory]
        [InlineData("google.com")]
        [InlineData("stackoverflow.com")] // "stack next se" is banned, but simple stackoverflow might be safe if not in list
        [InlineData("microsoft.com")]
        [InlineData("hello world")]
        public void ContainsBannedKeyword_SafeWords_ReturnsFalse(string input)
        {
            // Note: If "stackoverflow" triggers "stack next se", this test might fail if logic is loose.
            // But checking the list: "stack next se" is specific.
            // Let's check "stack" vs "stack next se".
            // The logic is `t.Contains(k)`.
            // "stack next se" is the keyword. "stackoverflow" does not contain "stack next se".

            bool result = EnforcementLogic.ContainsBannedKeyword(input);
            Assert.False(result, $"False positive detected for: {input}");
        }

        [Fact]
        public void ContainsBannedKeyword_CaseInsensitive_ReturnsTrue()
        {
            string input = "TwItTeR";
            bool result = EnforcementLogic.ContainsBannedKeyword(input);
            Assert.True(result, "Failed to detect mixed-case banned keyword.");
        }

        [Fact]
        public void ContainsBannedKeyword_EmptyString_ReturnsFalse()
        {
            bool result = EnforcementLogic.ContainsBannedKeyword("");
            Assert.False(result);
        }

        [Fact]
        public void ContainsBannedKeyword_Null_ReturnsFalse()
        {
            bool result = EnforcementLogic.ContainsBannedKeyword(null);
            Assert.False(result);
        }
    }
}
