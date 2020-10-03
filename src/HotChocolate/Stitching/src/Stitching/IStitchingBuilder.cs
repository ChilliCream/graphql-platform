using System;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Stitching.Merge;
using HotChocolate.Stitching.Merge.Rewriters;
using HotChocolate.Configuration;

namespace HotChocolate.Stitching
{
    public delegate IQueryExecutor ExecutorFactory(IServiceProvider services);

    public interface IStitchingBuilder
    {
        /// <summary>
        /// Add a remote schema document resolver.
        /// The remote schema registered like this will be queried through a
        /// <see cref="IRemoteRequestExecutor" /> obtained with the schema name
        /// from the <see cref="IStitchingContext" />.
        /// </summary>
        /// <param name="name">
        /// The name of the remote schema.
        /// </param>
        /// <param name="loadSchema">
        /// A delegate that resolves the schema document for this remote schema.
        /// </param>
        IStitchingBuilder AddSchema(
            NameString name,
            LoadSchemaDocument loadSchema);

        /// <summary>
        /// Add a query executor factory as remote schema.
        /// </summary>
        /// <param name="name">
        /// The name of the remote schema.
        /// </param>
        /// <param name="factory">
        /// A factory that creates an <see cref="IQueryExecutor" />
        /// which is used to query the remote schema.
        /// </param>
        IStitchingBuilder AddQueryExecutor(
            NameString name,
            ExecutorFactory factory);

        /// <summary>
        /// Add a extension document resolver.
        /// Extension documents can be used to extend merged types
        /// or even replace them.
        /// </summary>
        /// <param name="loadSchema">
        /// A delegate that resolves the schema extension document.
        /// </param>
        IStitchingBuilder AddExtensions(
            LoadSchemaDocument loadExtensions);

        /// <summary>
        /// Add a document rewriter in order to rewrite
        /// a remote schema document before it is being merged.
        /// </summary>
        /// <param name="rewriter">
        /// The document rewriter.
        /// </param>
        IStitchingBuilder AddDocumentRewriter(
            IDocumentRewriter rewriter);

        /// <summary>
        /// Add a type definition rewriter in order to rewrite a
        /// type definition on a remote schema document before
        /// it is being merged.
        /// </summary>
        /// <param name="rewriter">
        /// The type definition rewriter.
        /// </param>
        IStitchingBuilder AddTypeRewriter(
            ITypeRewriter rewriter);

        /// <summary>
        /// Add a type merge rule in order to define how a type is merged.
        /// </summary>
        /// <param name="factory">
        /// A factory that create the type merging rule.
        /// </param>
        [Obsolete("Use AddTypeMergeRule")]
        IStitchingBuilder AddMergeRule(
            MergeTypeRuleFactory factory);

        /// <summary>
        /// Add a type merge rule in order to define how a type is merged.
        /// </summary>
        /// <param name="factory">
        /// A factory that create the type merging rule.
        /// </param>
        IStitchingBuilder AddTypeMergeRule(
            MergeTypeRuleFactory factory);

        /// <summary>
        /// Add a directive merge rule in order to define
        /// how a directive is merged.
        /// </summary>
        /// <param name="factory">
        /// A factory that create the directive merging rule.
        /// </param>
        IStitchingBuilder AddDirectiveMergeRule(
            MergeDirectiveRuleFactory factory);

        /// <summary>
        /// Add a document rewriter that is executed on
        /// the merged schema document.
        /// </summary>
        /// <param name="rewrite">
        /// A delegate that is called to execute the
        /// rewrite document logic.
        /// </param>
        IStitchingBuilder AddMergedDocumentRewriter(
            Func<DocumentNode, DocumentNode> rewrite);

        /// <summary>
        /// Adds a schema visitor that is executed on
        /// the merged schema document.
        /// </summary>
        /// <param name="visit">
        /// A delegate that is called to execute the
        /// document visitation logic.
        /// </param>
        IStitchingBuilder AddMergedDocumentVisitor(
            Action<DocumentNode> visit);

        IStitchingBuilder AddSchemaConfiguration(
            Action<ISchemaConfiguration> configure);

        IStitchingBuilder AddExecutionConfiguration(
            Action<IQueryExecutionBuilder> configure);

        IStitchingBuilder SetExecutionOptions(
            IQueryExecutionOptionsAccessor options);

        IStitchingBuilder SetSchemaCreation(SchemaCreation creation);
    }
}
