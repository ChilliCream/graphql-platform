using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Validation
{
    public class MaxDepthRuleTests : DocumentValidatorVisitorTestBase
    {
        public MaxDepthRuleTests()
            : base(b => b.AddMaxExecutionDepthRule(3))
        {
        }

        [Fact]
        public void MaxDepth3_QueryWith4Levels_MaxDepthReached()
        {
            ExpectErrors(@"
                query {
                    level1_1 {
                        level2_1 {
                            level3_1 {
                                level4
                            }
                        }
                    }
                    level1_2
                    level1_2
                    {
                        level2_2
                        level2_3
                        {
                            level3_2
                        }
                    }
                }
            ");
        }

        [Fact]
        public void MaxDepth3_QueryWith4LevelsViaFragments_MaxDepthReached()
        {
            ExpectErrors(@"
                query {
                    level1_1 {
                        ... level2
                    }
                }

                fragment level2 on Level2
                {
                    level2_1 {
                        ... level3
                    }
                }

                fragment level3 on Level3
                {
                    level3_1 {
                        ... level4
                    }
                }

                fragment level4 on Level4
                {
                    level4
                }
            ");
        }

        [Fact]
        public void MaxDepth3_QueryWith4LevelsWithInlineFragment_MaxDepthReached()
        {
            ExpectErrors(@"
                query {
                    level1_1 {
                        ... on Level2 {
                            level2_1 {
                                ... on Level3 {
                                    level3_1
                                    {
                                        level4
                                    }
                                }
                            }
                        }
                    }
                }
            ");
        }

        [Fact]
        public void MaxDepth3_QueryWith3Levels_Valid()
        {
            ExpectValid(@"
                query {
                    level1_1 {
                        level2_1 {
                            level3_1
                        }
                    }
                    level1_2
                    level1_2
                    {
                        level2_2
                        level2_3
                        {
                            level3_2
                        }
                    }
                }
            ");
        }

        [Fact]
        public void MaxDepth3_QueryWith3LevelsViaFragments_Valid()
        {
            ExpectValid(@"
                query {
                    level1_1 {
                        ... level2
                    }
                }

                fragment level2 on Level2
                {
                    level2_1 {
                        ... level3
                    }
                }

                fragment level3 on Level3
                {
                    level3_1
                }
            ");
        }

        [Fact]
        public void MaxDepth3_QueryWith3LevelsWithInlineFragment_Valid()
        {
            // arrange
            ExpectValid(@"
                query {
                    level1_1 {
                        ... on Level2 {
                            level2_1 {
                                ... on Level3 {
                                    level3_1
                                }
                            }
                        }
                    }
                }
            ");
        }
    }
}
