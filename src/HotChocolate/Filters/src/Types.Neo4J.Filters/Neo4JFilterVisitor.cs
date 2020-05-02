using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Filters;

namespace HotChocolate.Types.Neo4J.Filters
{
    public class Neo4JFilterVisitor
        : FilterVisitorBase<Neo4JFilterVisitorContext>
    {
        protected Neo4JFilterVisitor()
        {
        }
        protected override ISyntaxVisitorAction Enter(
            ObjectValueNode node,
            Neo4JFilterVisitorContext context)
        {
            // Here you enter a new object  foo: <<YOU ARE HERE>>{    bar :"baz" }
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectValueNode node,
            Neo4JFilterVisitorContext context)
        {
            // Here you leave object  foo: {    bar :"baz" } <<YOU ARE HERE>>
            // you may want to combine the filters here. With expressions we combine the 
            // expession on the stack, lets say `foo.Qux == qux && foo.Baz == "baz"`, with
            // foo.Bar == "baz" to  `foo.Qux == qux && foo.Baz == "baz" && foo.Bar == "baz"`

            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            ObjectFieldNode node,
            Neo4JFilterVisitorContext context)
        {
            // Here you enter the field foo: { bar :"baz",  <<YOU ARE HERE>>qux:"Quux"  }

            // the base class here already pushs the Field on to the context `context.Operation" and
            // and the type of the field onto context.Types
            base.Enter(node, context);

            if (context.Operations.Peek() is FilterOperationField field)
            {
                // So in case of filtering we moved this logic into enter and leave handler. 
                // in this case we will just handle it as a switch case.
                // make things easier 
                switch (field.Operation.FilterKind)
                {
                    case FilterKind.Object:
                        // in case of an object you probably want to push somthing on the stack
                        // in the expression visitor, we combine the property with the expression
                        // on the stack and push it on the stack
                        // e.g. Property qux of type Bar
                        // on the stack is alread foo.Bar
                        // we push foo.Bar.Qux

                        // return continue to drill deeper. 
                        return Continue;

                    case FilterKind.Array:
                        // behaves similar to objects. this is really complex in case of Expressions
                        // We create a new "Clousre" becasue we have a new parameter
                        // e.g. x => x.Foo.Any(y => y.Bar == "test");

                        // return continue to drill deeper. 
                        return Continue;
                }

                // in case we did not find a enter or leave method of the FilterKind there might be
                // a operation handler for it. This should be moved out into separat classes
                switch (field.Operation.FilterKind)
                {
                    case FilterKind.String:
                        switch (field.Operation.Kind)
                        {
                            case FilterOperationKind.Equals:
                                // handle equals of string
                                break;
                        }
                        break;
                }
                // we want to SkipAndLeave. We do not want to digg deeper as we found the operation
                return SkipAndLeave;
            }
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectFieldNode node,
            Neo4JFilterVisitorContext context)
        {
            // Here you enter the field foo: { bar :"baz",  qux:"Quux"<<YOU ARE HERE>>  }

            if (context.Operations.Peek() is FilterOperationField field)
            {
                switch (field.Operation.FilterKind)
                {
                    case FilterKind.Object:
                        // in case of an object you probably want to clean somthing from the stack
                        // in the expression visitor, we combine the expressions. we pop the latest
                        // expression for the stack e.g. foo.Bar.Qux == "test" and add a nullcheck
                        // in front of it: foo.Bar.Qux != null && foo.Bar.Qux == "test"


                        // do not return

                        break;
                    case FilterKind.Array:
                        // behaves similar to objects. In case of expressions we pop the closure
                        // from the stack and apply it to the list operator. so, from
                        // foo.bar == "test" we create list.Any(x => foo.bar == "test");
                        break;
                }
            }
            // this pop the field and the type from the context
            return base.Leave(node, context);
        }


        protected override ISyntaxVisitorAction Enter(
            ListValueNode node,
            Neo4JFilterVisitorContext context)
        {
            // Here you enter the list foo: { <<YOU ARE HERE>>OR:[]  }

            // You probably want to add smth to the stack here 
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ListValueNode node,
            Neo4JFilterVisitorContext context)
        {
            // Here you leave the list foo: { OR:[]<<YOU ARE HERE>>  }

            // there are currently three different possibilties for having an list as an input
            // stringIs_in:["foo","bar"], OR:[] and AND:[]
            // here we only have to care about 2. the _in filters are already handled in the field
            // if we find a FilterOperationKind.In there, we already "SkipAndLeave"
            //
            return Continue;
        }



        public static Neo4JFilterVisitor Default = new Neo4JFilterVisitor();
    }
}
