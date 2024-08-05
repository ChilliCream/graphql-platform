namespace HotChocolate.Fusion.Types;

public interface ICompositeField
{
    string Name { get; }
}

public interface ICompositeOutputField : ICompositeField
{

}
