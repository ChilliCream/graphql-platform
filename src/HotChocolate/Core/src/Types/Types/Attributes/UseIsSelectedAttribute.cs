using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

/// <summary>
/// Adds a middleware that checks if the specified fields are selected.
/// This middleware adds a local state called `isSelected`.
/// <code>
/// <![CDATA[
/// [UseSelected("address", "items")]
/// public async Task<Order> GetOrderAsync(int id, [LocalState] bool isSelected)
/// {
///     // resolver code ...
/// }
/// ]]>
/// </code>
/// </summary>
public class UseIsSelectedAttribute : ObjectFieldDescriptorAttribute
{
    /// <summary>
    /// Adds a middleware that checks if the specified fields are selected.
    /// This middleware adds a local state called `isSelected`.
    /// <code>
    /// <![CDATA[
    /// [UseSelected("address", "items")]
    /// public async Task<Order> GetOrderAsync(int id, [LocalState] bool isSelected)
    /// {
    ///     // resolver code ...
    /// }
    /// ]]>
    /// </code>
    /// </summary>
    /// <param name="fieldName">
    /// The field name that we check for.
    /// </param>
    public UseIsSelectedAttribute(string fieldName)
    {
        FieldNames = [fieldName,];
    }
    
    /// <summary>
    /// Adds a middleware that checks if the specified fields are selected.
    /// This middleware adds a local state called `isSelected`.
    /// <code>
    /// <![CDATA[
    /// [UseSelected("address", "items")]
    /// public async Task<Order> GetOrderAsync(int id, [LocalState] bool isSelected)
    /// {
    ///     // resolver code ...
    /// }
    /// ]]>
    /// </code>
    /// </summary>
    /// <param name="fieldName1">
    /// The first field name we check for.
    /// </param>
    /// <param name="fieldName2">
    /// The second field name we check for.
    /// </param>
    public UseIsSelectedAttribute(string fieldName1, string fieldName2)
    {
        FieldNames = [fieldName1, fieldName2,];
    }
    
    /// <summary>
    /// Adds a middleware that checks if the specified fields are selected.
    /// This middleware adds a local state called `isSelected`.
    /// <code>
    /// <![CDATA[
    /// [UseSelected("address", "items")]
    /// public async Task<Order> GetOrderAsync(int id, [LocalState] bool isSelected)
    /// {
    ///     // resolver code ...
    /// }
    /// ]]>
    /// </code>
    /// </summary>
    /// <param name="fieldName1">
    /// The first field name we check for.
    /// </param>
    /// <param name="fieldName2">
    /// The second field name we check for.
    /// </param>
    /// <param name="fieldName3">
    /// The third field name we check for.
    /// </param>
    public UseIsSelectedAttribute(string fieldName1, string fieldName2, string fieldName3)
    {
        FieldNames = [fieldName1, fieldName2, fieldName3,];
    }
    
    /// <summary>
    /// Adds a middleware that checks if the specified fields are selected.
    /// This middleware adds a local state called `isSelected`.
    /// <code>
    /// <![CDATA[
    /// [UseSelected("address", "items")]
    /// public async Task<Order> GetOrderAsync(int id, [LocalState] bool isSelected)
    /// {
    ///     // resolver code ...
    /// }
    /// ]]>
    /// </code>
    /// </summary>
    /// <param name="fieldNames">
    /// The field names we check for.
    /// </param>
    public UseIsSelectedAttribute(params string[] fieldNames)
    {
        FieldNames = fieldNames;
    }
    
    /// <summary>
    /// Gets the field names we check for.
    /// </summary>
    public string[] FieldNames { get; }

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        switch (FieldNames.Length)
        {
            case 1:
            {
                var fieldName = FieldNames[0];
            
                descriptor.Use(
                    next => async ctx =>
                    {
                        var isSelected = ctx.IsSelected(fieldName);
                        ctx.SetLocalState(nameof(isSelected), isSelected);
                        await next(ctx);
                    });
                break;
            }

            case 2:
            {
                var fieldName1 = FieldNames[0];
                var fieldName2 = FieldNames[1];
            
                descriptor.Use(
                    next => async ctx =>
                    {
                        var isSelected = ctx.IsSelected(fieldName1, fieldName2);
                        ctx.SetLocalState(nameof(isSelected), isSelected);
                        await next(ctx);
                    });
                break;
            }

            case 3:
            {
                var fieldName1 = FieldNames[0];
                var fieldName2 = FieldNames[1];
                var fieldName3 = FieldNames[2];
            
                descriptor.Use(
                    next => async ctx =>
                    {
                        var isSelected = ctx.IsSelected(fieldName1, fieldName2, fieldName3);
                        ctx.SetLocalState(nameof(isSelected), isSelected);
                        await next(ctx);
                    });
                break;
            }

            case > 3:
            {
                var fieldNames = new HashSet<string>(FieldNames);
            
                descriptor.Use(
                    next => async ctx =>
                    {
                        var isSelected = ctx.IsSelected(fieldNames);
                        ctx.SetLocalState(nameof(isSelected), isSelected);
                        await next(ctx);
                    });
                break;
            }
        }
    }
}