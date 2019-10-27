using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace  StrawberryShake.Client.GraphQL
{
    public class CreateReviewOperation
        : IOperation<ICreateReview>
    {
        private bool _modified_episode;
        private bool _modified_review;

        private Episode _value_episode;
        private ReviewInput _value_review;

        public string Name => "createReview";

        public IDocument Document => Queries.Default;

        public Type ResultType => typeof(ICreateReview);

        public Episode Episode
        {
            get => _value_episode;
            set
            {
                _value_episode = value;
                _modified_episode = true;
            }
        }

        public ReviewInput Review
        {
            get => _value_review;
            set
            {
                _value_review = value;
                _modified_review = true;
            }
        }

        public IReadOnlyList<VariableValue> GetVariableValues()
        {
            var variables = new List<VariableValue>();

            if(_modified_episode)
            {
                variables.Add(new VariableValue("episode", "Episode", Episode));
            }

            if(_modified_review)
            {
                variables.Add(new VariableValue("review", "ReviewInput", Review));
            }

            return variables;
        }
    }
}
