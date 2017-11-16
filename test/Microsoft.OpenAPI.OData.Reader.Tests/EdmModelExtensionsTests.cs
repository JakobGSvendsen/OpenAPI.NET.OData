﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.IO;
using Microsoft.OData.Edm;
using Microsoft.OpenApi.Models;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.OpenApi.OData.Tests
{
    public class EdmModelExtensionsTests
    {
        private ITestOutputHelper _output;
        public EdmModelExtensionsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void GetDescriptionReturnsNullForElementWithoutCoreDescription()
        {
            // Arrange
            IEdmModel model = EdmModelHelper.BasicEdmModel;
            IEdmSingleton me = model.EntityContainer.FindSingleton("Me");
            Assert.NotNull(me); // Guard

            // Act
            string description = model.GetDescription(me);

            // Assert
            Assert.Null(description);
        }

        [Fact]
        public void GetDescriptionReturnsAnnotationForElementWithCoreDescription()
        {
            // Arrange
            IEdmModel model = EdmModelHelper.BasicEdmModel;
            IEdmEntitySet people = model.EntityContainer.FindEntitySet("People");
            Assert.NotNull(people); // Guard

            // Act
            string description = model.GetDescription(people);

            // Assert
            Assert.Equal("People's description.", description);
        }

        [Fact]
        public void GetLongDescriptionReturnsAnnotationForElementWithCoreLongDescription()
        {
            // Arrange
            IEdmModel model = EdmModelHelper.BasicEdmModel;
            IEdmEntitySet people = model.EntityContainer.FindEntitySet("People");
            Assert.NotNull(people); // Guard

            // Act
            string description = model.GetLongDescription(people);

            // Assert
            Assert.Equal("People's Long description.", description);
        }
    }
}
