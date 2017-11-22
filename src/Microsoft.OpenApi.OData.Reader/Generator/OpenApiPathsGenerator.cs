﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.OData.Edm;
using Microsoft.OpenApi.Models;

namespace Microsoft.OpenApi.OData.Generator
{
    /// <summary>
    /// Extension methods to create <see cref="OpenApiPaths"/> by <see cref="IEdmModel"/>.
    /// </summary>
    internal static class OpenApiPathsGenerator
    {
        /// <summary>
        /// Create a <see cref="OpenApiPaths"/>
        /// The value of paths is a Paths Object.
        /// It is the main source of information on how to use the described API.
        /// It consists of name/value pairs whose name is a path template relative to the service root URL,
        /// and whose value is a Path Item Object.
        /// </summary>
        /// <param name="model">The Edm model.</param>
        /// <param name="settings">The convert settings.</param>
        /// <returns>The created <see cref="OpenApiPaths"/> object.</returns>
        public static OpenApiPaths CreatePaths(this IEdmModel model, OpenApiConvertSettings settings)
        {
            if (model == null)
            {
                throw Error.ArgumentNull(nameof(model));
            }

            if (settings == null)
            {
                throw Error.ArgumentNull(nameof(settings));
            }

            // Due to the power and flexibility of OData a full representation of all service capabilities
            // in the Paths Object is typically not feasible, so this mapping only describes the minimum
            // information desired in the Paths Object.
            OpenApiPaths paths = new OpenApiPaths();
            foreach (var item in model.CreatePathItems(settings))
            {
                paths.Add(item.Key, item.Value);
            }

            return paths;
        }
    }
}
