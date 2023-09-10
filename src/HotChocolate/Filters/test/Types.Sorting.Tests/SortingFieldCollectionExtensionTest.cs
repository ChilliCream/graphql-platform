using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Sorting.Extensions;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types.Sorting;

[Obsolete]
public class SortingFieldCollectionExtensionTest
{
    private readonly Expression<Func<Foo, string>> _property;
    private readonly PropertyInfo _propertyInfo;
    private readonly IDescriptorContext _descriptorContext;

    public SortingFieldCollectionExtensionTest()
    {
        _property = x => x.Bar;
        _propertyInfo = (PropertyInfo)_property.ExtractMember();
        _descriptorContext = DescriptorContext.Create();
    }

    [Fact]
    public void GetOrAddDescriptor_Argument_Fields()
    {
        //arrange
        var factory =
            () => SortOperationDescriptor.CreateOperation(_propertyInfo, _descriptorContext);

        //act
        //assert
        var assertNullException =
            Assert.Throws<ArgumentNullException>(
                () => SortingFieldCollectionExtensions.GetOrAddDescriptor(
                    null, _propertyInfo, factory));
        Assert.Equal("fields", assertNullException.ParamName);
    }


    [Fact]
    public void GetOrAddDescriptor_Argument_PropertyInfo()
    {
        //arrange
        var factory =
            () => SortOperationDescriptor.CreateOperation(_propertyInfo, _descriptorContext);
        IList<SortOperationDescriptorBase> list = new List<SortOperationDescriptorBase>();

        //act
        //assert
        var assertNullException =
            Assert.Throws<ArgumentNullException>(() => list.GetOrAddDescriptor(null, factory));
        Assert.Equal("propertyInfo", assertNullException.ParamName);
    }


    [Fact]
    public void GetOrAddDescriptor_Argument_Factory()
    {
        //arrange
        IList<SortOperationDescriptorBase> list = new List<SortOperationDescriptorBase>();

        //act
        //assert
        var assertNullException =
            Assert.Throws<ArgumentNullException>(
                () => list.GetOrAddDescriptor<SortOperationDescriptor>(
                    _propertyInfo, null));

        Assert.Equal("valueFactory", assertNullException.ParamName);
    }

    [Fact]
    public void GetOrAddDescriptor_Should_AddDescriptorIfNotExists()
    {
        //arrange
        IList<SortOperationDescriptorBase> list = new List<SortOperationDescriptorBase>();
        var descriptor =
            SortOperationDescriptor.CreateOperation(_propertyInfo, _descriptorContext);
        var valueFactory = () => descriptor;

        //act
        var result =
            list.GetOrAddDescriptor(_propertyInfo, valueFactory);

        //assert
        Assert.Single(list);
        Assert.Same(descriptor, list[0]);
        Assert.Same(descriptor, result);
    }

    [Fact]
    public void GetOrAddDescriptor_Should_ReturnDescriptorIfAlreadyExists()
    {
        //arrange
        IList<SortOperationDescriptorBase> list = new List<SortOperationDescriptorBase>();
        var descriptorShouldNotBeRemoved =
            SortOperationDescriptor.CreateOperation(_propertyInfo, _descriptorContext);
        var newDescriptorShouldNotHaveAnyEffect =
            SortOperationDescriptor.CreateOperation(_propertyInfo, _descriptorContext);
        var valueFactory = () => newDescriptorShouldNotHaveAnyEffect;

        list.Add(descriptorShouldNotBeRemoved);

        //act
        var result =
            list.GetOrAddDescriptor(_propertyInfo, valueFactory);

        //assert
        Assert.Single(list);
        Assert.Same(descriptorShouldNotBeRemoved, list[0]);
        Assert.Same(descriptorShouldNotBeRemoved, result);
        Assert.NotSame(newDescriptorShouldNotHaveAnyEffect, result);
    }

    [Fact]
    public void GetOrAddDescriptor_Should_ReplaceDescriptorIfDifferentType()
    {
        //arrange
        IList<SortOperationDescriptorBase> list = new List<SortOperationDescriptorBase>();
        var descriptorToRemove =
            IgnoredSortingFieldDescriptor.CreateOperation(_propertyInfo, _descriptorContext);
        var descriptorToAdd =
            SortOperationDescriptor.CreateOperation(_propertyInfo, _descriptorContext);
        var valueFactory = () => descriptorToAdd;

        list.Add(descriptorToRemove);

        //act
        var result =
            list.GetOrAddDescriptor(_propertyInfo, valueFactory);

        //assert
        Assert.Single(list);
        Assert.Same(descriptorToAdd, list[0]);
        Assert.Same(descriptorToAdd, result);
        Assert.NotSame(descriptorToRemove, result);
    }


    private sealed class Foo
    {
        public string Bar { get; set; }
    }

}