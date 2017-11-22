﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.OData.Edm;
using Microsoft.OpenApi.Models;

namespace Microsoft.OpenApi.OData.Generator
{
    /// <summary>
    /// Class to create <see cref="OpenApiPathItem"/> by Edm elements.
    /// </summary>
    internal static class OpenApiPathItemGenerator
    {
        /// <summary>
        /// Create a map of <see cref="OpenApiPathItem"/>.
        /// </summary>
        /// <param name="model">The Edm model.</param>
        /// <param name="settings">The convert settings.</param>
        /// <returns>The created map of <see cref="OpenApiPathItem"/>.</returns>
        public static IDictionary<string, OpenApiPathItem> CreatePathItems(this IEdmModel model,
            OpenApiConvertSettings settings)
        {
            if (model == null)
            {
                throw Error.ArgumentNull(nameof(model));
            }

            if (settings == null)
            {
                throw Error.ArgumentNull(nameof(settings));
            }

            IDictionary<string, OpenApiPathItem> pathItems = new Dictionary<string, OpenApiPathItem>();
            if (model.EntityContainer == null)
            {
                return pathItems;
            }

            var boundOperations = new Dictionary<IEdmTypeReference, IEdmOperation>();
            foreach (var edmOperation in model.SchemaElements.OfType<IEdmOperation>().Where(e => e.IsBound))
            {
                IEdmOperationParameter bindingParameter = edmOperation.Parameters.First();
                boundOperations.Add(bindingParameter.Type, edmOperation);
            }
            /*
            // visit all elements in the container
            foreach (var element in model.EntityContainer.Elements)
            {
                switch (element.ContainerElementKind)
                {
                    case EdmContainerElementKind.EntitySet:
                        IEdmEntitySet entitySet = (IEdmEntitySet)element;
                        foreach (var item in model.CreatePathItems(entitySet, settings))
                        {
                            pathItems.Add(item.Key, item.Value);
                        }

                        foreach (var item in entitySet.CreateOperationPathItems(entitySet, operations))
                        {
                            pathItems.Add(item.Key, item.Value);
                        }
                        break;

                    case EdmContainerElementKind.Singleton:
                        IEdmSingleton singleton = element as IEdmSingleton;
                        if (singleton != null)
                        {
                            foreach (var item in _nsGenerator.CreatePaths(singleton))
                            {
                                _paths.Add(item.Key, item.Value);
                            }
                        }
                        break;

                    case EdmContainerElementKind.FunctionImport:
                        IEdmFunctionImport functionImport = element as IEdmFunctionImport;
                        if (functionImport != null)
                        {
                            var functionImportPathItem = functionImport.CreatePathItem();

                            _paths.Add(functionImport.CreatePathItemName(), functionImportPathItem);
                        }
                        break;

                    case EdmContainerElementKind.ActionImport:
                        IEdmActionImport actionImport = element as IEdmActionImport;
                        if (actionImport != null)
                        {
                            var functionImportPathItem = actionImport.CreatePathItem();
                            _paths.Add(actionImport.CreatePathItemName(), functionImportPathItem);
                        }
                        break;
                }
            }*/

                // entity set path items
            foreach (var item in model.CreateEntitySetPathItems(settings, boundOperations))
            {
                pathItems.Add(item.Key, item.Value);
            }

            // singleton path items
            foreach (var item in model.CreateSingletonPathItems(settings, boundOperations))
            {
                pathItems.Add(item.Key, item.Value);
            }

            // operation import path items
            foreach (var item in model.CreateOperationImportPathItems(settings))
            {
                pathItems.Add(item.Key, item.Value);
            }

            return pathItems;
        }

        private static IDictionary<string, OpenApiPathItem> CreateEntitySetPathItems(this IEdmModel model,
            OpenApiConvertSettings settings,
            IDictionary<IEdmTypeReference, IEdmOperation> operations)
        {
            IDictionary<string, OpenApiPathItem> pathItems = new Dictionary<string, OpenApiPathItem>();

            if (model.EntityContainer != null)
            {
                foreach (var entitySet in model.EntityContainer.EntitySets())
                {
                    foreach (var item in CreatePathItems(entitySet))
                    {
                        pathItems.Add(item.Key, item.Value);
                    }

                    foreach (var item in CreateOperationPathItems(entitySet, operations))
                    {
                        pathItems.Add(item.Key, item.Value);
                    }
                }
            }

            return pathItems;
        }

        private static IDictionary<string, OpenApiPathItem> CreateSingletonPathItems(this IEdmModel model,
            OpenApiConvertSettings settings,
            IDictionary<IEdmTypeReference, IEdmOperation> operations)
        {
            IDictionary<string, OpenApiPathItem> pathItems = new Dictionary<string, OpenApiPathItem>();

            if (model.EntityContainer != null)
            {
                foreach (var singleton in model.EntityContainer.Singletons())
                {
                    foreach (var item in CreatePathItems(singleton))
                    {
                        pathItems.Add(item.Key, item.Value);
                    }

                    foreach (var item in CreateOperationPathItems(singleton, operations))
                    {
                        pathItems.Add(item.Key, item.Value);
                    }
                }
            }

            return pathItems;
        }

        private static IDictionary<string, OpenApiPathItem> CreateOperationImportPathItems(this IEdmModel model,
            OpenApiConvertSettings settings)
        {
            IDictionary<string, OpenApiPathItem> pathItems = new Dictionary<string, OpenApiPathItem>();

            if (model.EntityContainer != null)
            {
                foreach (var operationImport in model.EntityContainer.OperationImports())
                {
                    var operationPathItem = CreatePathItem(operationImport);
                    pathItems.Add(CreatePathItemName(operationImport), operationPathItem);
                }
            }

            return pathItems;
        }

        /// <summary>
        /// Path items for Entity Sets.
        /// Each entity set is represented as a name/value pair
        /// whose name is the service-relative resource path of the entity set prepended with a forward slash,
        /// and whose value is a Path Item Object.
        /// </summary>
        /// <param name="entitySet">The Edm entity set.</param>
        /// <returns>The path items.</returns>
        public static IDictionary<string, OpenApiPathItem> CreatePathItems(this IEdmEntitySet entitySet)
        {
            if (entitySet == null)
            {
                throw Error.ArgumentNull(nameof(entitySet));
            }

            IDictionary<string, OpenApiPathItem> paths = new Dictionary<string, OpenApiPathItem>();

            // entity set
            OpenApiPathItem pathItem = new OpenApiPathItem();

            pathItem.AddOperation(OperationType.Get, entitySet.CreateGetOperationForEntitySet());

            pathItem.AddOperation(OperationType.Post, entitySet.CreatePostOperationForEntitySet());

            paths.Add("/" + entitySet.Name, pathItem);

            // entity
            string entityPathName = entitySet.CreatePathNameForEntity();
            pathItem = new OpenApiPathItem();

            pathItem.AddOperation(OperationType.Get, entitySet.CreateGetOperationForEntity());

            pathItem.AddOperation(OperationType.Patch, entitySet.CreatePatchOperationForEntity());

            pathItem.AddOperation(OperationType.Delete, entitySet.CreateDeleteOperationForEntity());

            paths.Add(entityPathName, pathItem);

            return paths;
        }

        /// <summary>
        /// Each singleton is represented as a name/value pair whose name is the service-relative resource
        /// paht of the singleton prepended with a forward slash, whose value is <see cref="OpenApiPathItem"/>
        /// describing the allowed operations on this singleton.
        /// </summary>
        /// <param name="singleton">The singleton.</param>
        /// <returns>The name/value pairs describing the allowed operations on this singleton.</returns>
        private static IDictionary<string, OpenApiPathItem> CreatePathItems(IEdmSingleton singleton)
        {
            if (singleton == null)
            {
                throw Error.ArgumentNull(nameof(singleton));
            }

            IDictionary<string, OpenApiPathItem> paths = new Dictionary<string, OpenApiPathItem>();

            // Singleton
            string entityPathName = singleton.CreatePathNameForSingleton();
            OpenApiPathItem pathItem = new OpenApiPathItem();

            // Retrieve a singleton.
            pathItem.AddOperation(OperationType.Get, singleton.CreateGetOperationForSingleton());

            // Update a singleton
            pathItem.AddOperation(OperationType.Patch, singleton.CreatePatchOperationForSingleton());

            paths.Add(entityPathName, pathItem);

            return paths;
        }

        private static IDictionary<string, OpenApiPathItem> CreateOperationPathItems(IEdmNavigationSource navigationSource,
            IDictionary<IEdmTypeReference, IEdmOperation> boundOperations)
        {
            IDictionary<string, OpenApiPathItem> operationPathItems = new Dictionary<string, OpenApiPathItem>();

            IEnumerable<IEdmOperation> operations;
            IEdmEntitySet entitySet = navigationSource as IEdmEntitySet;
            // collection bound
            if (entitySet != null)
            {
                operations = FindOperations(navigationSource.EntityType(), boundOperations, collection: true);
                foreach (var operation in operations)
                {
                    OpenApiPathItem openApiOperation = CreatePathItem(operation);
                    string operationPathName = CreatePathItemName(operation);
                    operationPathItems.Add("/" + navigationSource.Name + operationPathName, openApiOperation);
                }
            }

            // non-collection bound
            operations = FindOperations(navigationSource.EntityType(), boundOperations, collection: false);
            foreach (var operation in operations)
            {
                OpenApiPathItem openApiOperation = CreatePathItem(operation);
                string operationPathName = CreatePathItemName(operation);

                string temp;
                if (entitySet != null)
                {
                    temp = entitySet.CreatePathNameForEntity();
                }
                else
                {
                    temp = "/" + navigationSource.Name;
                }
                operationPathItems.Add(temp + operationPathName, openApiOperation);
            }

            return operationPathItems;
        }

        private static IEnumerable<IEdmOperation> FindOperations(IEdmEntityType entityType,
            IDictionary<IEdmTypeReference, IEdmOperation> operations,
            bool collection)
        {
            string fullTypeName = collection ? "Collection(" + entityType.FullName() + ")" :
                entityType.FullName();

            foreach (var item in operations)
            {
                if (item.Key.FullName() == fullTypeName)
                {
                    yield return item.Value;
                }
            }
        }

        private static OpenApiPathItem CreatePathItem(this IEdmOperationImport operationImport)
        {
            if (operationImport.Operation.IsAction())
            {
                return CreatePathItem((IEdmActionImport)operationImport);
            }

            return CreatePathItem((IEdmFunctionImport)operationImport);
        }

        private static OpenApiPathItem CreatePathItem(this IEdmOperation operation)
        {
            if (operation.IsAction())
            {
                return CreatePathItem((IEdmAction)operation);
            }

            return CreatePathItem((IEdmFunction)operation);
        }

        public static OpenApiPathItem CreatePathItem(IEdmActionImport actionImport)
        {
            return CreatePathItem(actionImport.Action);
        }

        public static OpenApiPathItem CreatePathItem(IEdmAction action)
        {
            OpenApiPathItem pathItem = new OpenApiPathItem();

            OpenApiOperation post = new OpenApiOperation
            {
                Summary = "Invoke action " + action.Name,
                Tags = CreateTags(action),
                Parameters = action.CreateParameters(),
                Responses = action.CreateResponses()
            };

            pathItem.AddOperation(OperationType.Post, post);
            return pathItem;
        }

        public static OpenApiPathItem CreatePathItem(IEdmFunctionImport functionImport)
        {
            return CreatePathItem(functionImport.Function);
        }

        public static OpenApiPathItem CreatePathItem(IEdmFunction function)
        {
            OpenApiPathItem pathItem = new OpenApiPathItem();
            OpenApiOperation get = new OpenApiOperation
            {
                Summary = "Invoke function " + function.Name,
                Tags = CreateTags(function),
                Parameters = function.CreateParameters(),
                Responses = function.CreateResponses()
            };

            pathItem.AddOperation(OperationType.Get, get);
            return pathItem;
        }

        public static string CreatePathItemName(IEdmActionImport actionImport)
        {
            return CreatePathItemName(actionImport.Action);
        }

        public static string CreatePathItemName(IEdmAction action)
        {
            return "/" + action.Name;
        }

        public static string CreatePathItemName(IEdmFunctionImport functionImport)
        {
            return CreatePathItemName(functionImport.Function);
        }

        public static string CreatePathItemName(IEdmFunction function)
        {
            StringBuilder functionName = new StringBuilder("/" + function.Name + "(");

            functionName.Append(String.Join(",",
                function.Parameters.Select(p => p.Name + "=" + "{" + p.Name + "}")));
            functionName.Append(")");

            return functionName.ToString();
        }

        public static string CreatePathItemName(IEdmOperationImport operationImport)
        {
            if (operationImport.Operation.IsAction())
            {
                return CreatePathItemName((IEdmActionImport)operationImport);
            }

            return CreatePathItemName((IEdmFunctionImport)operationImport);
        }

        public static string CreatePathItemName(IEdmOperation operation)
        {
            if (operation.IsAction())
            {
                return CreatePathItemName((IEdmAction)operation);
            }

            return CreatePathItemName((IEdmFunction)operation);
        }

        private static IList<string> CreateTags(IEdmOperationImport operationImport)
        {
            if (operationImport.EntitySet != null)
            {
                var pathExpression = operationImport.EntitySet as IEdmPathExpression;
                if (pathExpression != null)
                {
                    return new List<string>
                    {
                        PathAsString(pathExpression.PathSegments)
                    };
                }
            }

            return null;
        }

        private static IList<OpenApiTag> CreateTags(IEdmOperation operation)
        {
            if (operation.EntitySetPath != null)
            {
                var pathExpression = operation.EntitySetPath as IEdmPathExpression;
                if (pathExpression != null)
                {
                    return new List<OpenApiTag>
                    {
                        new OpenApiTag
                        {
                            Name = PathAsString(pathExpression.PathSegments)
                        }
                    };
                }
            }

            return null;
        }

        internal static string PathAsString(IEnumerable<string> path)
        {
            return String.Join("/", path);
        }
    }
}
