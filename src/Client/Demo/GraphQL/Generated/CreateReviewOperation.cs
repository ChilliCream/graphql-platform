using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
    public class CreateReviewOperation
        : IOperation<ICreateReview>
    {
        public string Name => "createReview";

        public IDocument Document => Queries.Default;

        public OperationKind Kind => OperationKind.Mutation;

        public Type ResultType => typeof(ICreateReview);

        public Optional<Episode> Episode { get; set; }

        public Optional<ReviewInput> Review { get; set; }

        public IReadOnlyList<VariableValue> GetVariableValues()
        {
            var variables = new List<VariableValue>();

            if (Episode.HasValue)
            {
                variables.Add(new VariableValue("episode", "Episode", Episode.Value));
            }

            if (Review.HasValue)
            {
                variables.Add(new VariableValue("review", "ReviewInput", Review.Value));
            }

            return variables;
        }
    }
}
