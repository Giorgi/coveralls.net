﻿using System;
using System.Collections.Generic;
using csmacnz.Coveralls.Data;
using csmacnz.Coveralls.GitDataResolvers;
using csmacnz.Coveralls.Ports;
using csmacnz.Coveralls.Tests.TestAdapters;
using Xunit;

namespace csmacnz.Coveralls.Tests.GitDataResolvers
{
    public class AppVeyorGitDataResolverTests
    {
        [Fact]
        public void CanProvideDataNoEnvironmentVariablesSetReturnsFalse()
        {
            var sut = new AppVeyorGitDataResolver(new TestEnvironmentVariables());

            var canProvideData = sut.CanProvideData();

            Assert.False(canProvideData);
        }

        [Fact]
        public void CanProvideDataAppVeyorEnvironmentVariableSetToFalseReturnsFalse()
        {
            IEnvironmentVariables variables = new TestEnvironmentVariables(new Dictionary<string, string>
            {
                { "APPVEYOR", "False" }
            });

            var sut = new AppVeyorGitDataResolver(variables);

            var canProvideData = sut.CanProvideData();

            Assert.False(canProvideData);
        }

        [Fact]
        public void CanProvideDataAppVeyorEnvironmentVariablesSetReturnsTrue()
        {
            IEnvironmentVariables variables = new TestEnvironmentVariables(new Dictionary<string, string>
            {
                { "APPVEYOR", "True" }
            });

            var sut = new AppVeyorGitDataResolver(variables);

            var canProvideData = sut.CanProvideData();

            Assert.True(canProvideData);
        }

        [Fact]
        public void GenerateDataNoEnviromentDataReturnsEmptyGitData()
        {
            IEnvironmentVariables variables = new TestEnvironmentVariables(new Dictionary<string, string>
            {
                { "APPVEYOR", "True" }
            });

            var sut = new AppVeyorGitDataResolver(variables);

            var gitData = sut.GenerateData();

            Assert.NotNull(gitData);
            Assert.True(gitData!.Value.IsItem1);
        }

        public class GenerateData
        {
            private readonly string _expectedBranch;
            private readonly string _expectedEmail;
            private readonly string _expectedId;
            private readonly string _expectedMessage;
            private readonly string _expectedName;
            private readonly GitData _gitData;

            public GenerateData()
            {
                _expectedId = Guid.NewGuid().ToString();
                _expectedName = "Test User Name";
                _expectedEmail = "email@example.com";
                _expectedMessage = "Add a new widget\n* some code\n* some tests";
                _expectedBranch = "feature";

                IEnvironmentVariables variables = new TestEnvironmentVariables(new Dictionary<string, string>
                {
                    { "APPVEYOR", "True" },
                    { "APPVEYOR_REPO_COMMIT", _expectedId },
                    { "APPVEYOR_REPO_COMMIT_AUTHOR", _expectedName },
                    { "APPVEYOR_REPO_COMMIT_AUTHOR_EMAIL", _expectedEmail },
                    { "APPVEYOR_REPO_COMMIT_MESSAGE", _expectedMessage },
                    { "APPVEYOR_REPO_BRANCH", _expectedBranch }
                });

                var sut = new AppVeyorGitDataResolver(variables);

                var generatedData = sut.GenerateData();
                var data = generatedData.HasValue
                    ? generatedData.Value.Match(g => g, c => (GitData?)null)
                    : null;

                _gitData = data ?? throw new Exception("Expected GitData");
            }

            [Fact]
            public void CommitIdSetCorrectly()
            {
                Assert.NotNull(_gitData);
                Assert.NotNull(_gitData.Head);
                Assert.Equal(_expectedId, _gitData.Head!.Id);
            }

            [Fact]
            public void NameSetCorrectly()
            {
                Assert.NotNull(_gitData);
                Assert.NotNull(_gitData.Head);
                Assert.Equal(_expectedName, _gitData.Head!.AuthorName);
                Assert.Equal(_expectedName, _gitData.Head!.CommitterName);
            }

            [Fact]
            public void EmailSetCorrectly()
            {
                Assert.NotNull(_gitData);
                Assert.NotNull(_gitData.Head);
                Assert.Equal(_expectedEmail, _gitData.Head!.AuthorEmail);
                Assert.Equal(_expectedEmail, _gitData.Head!.ComitterEmail);
            }

            [Fact]
            public void MessageSetCorrectly()
            {
                Assert.NotNull(_gitData);
                Assert.NotNull(_gitData.Head);
                Assert.Equal(_expectedMessage, _gitData.Head!.Message);
            }

            [Fact]
            public void BranchSetCorrectly()
            {
                Assert.NotNull(_gitData);
                Assert.Equal(_expectedBranch, _gitData.Branch);
            }
        }
    }
}
