﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using FluentAssertions;
using SOS.Lib.Models.Search;
using SOS.Lib.Models.Shared;
using Xunit;

namespace SOS.Lib.UnitTests.Models.Search
{
    public class SearchFilterTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public void A_cloned_filter_is_equivalent_to_the_original_filter()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            var currentDate = DateTime.Now;
            var filter = new SearchFilter
            {
                CountyIds = new[] {1, 2, 3},
                GeometryFilter = new GeometryFilter
                {
                    MaxDistanceFromPoint = 50,
                    UsePointAccuracy = true,
                    Geometry = new InputGeometry
                    {
                        Coordinates = new ArrayList(){ new[] { new double[] { 1, 2 }, new double[] { 3, 4 } } } 
                    }
                },
                EndDate = currentDate
            };

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var clonedFilter = filter.Clone();

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            clonedFilter.Should().BeEquivalentTo(filter);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void A_cloned_filter_that_is_modified_is_not_equivalent_to_the_original_filter()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            var currentDate = DateTime.Now;
            var filter = new SearchFilter
            {
                CountyIds = new[] { 1, 2, 3 },
                GeometryFilter = new GeometryFilter
                {
                    MaxDistanceFromPoint = 50,
                    UsePointAccuracy = true,
                    Geometry = new InputGeometry
                    {
                        Coordinates = new ArrayList(){ new[] { new double[] { 1, 2 }, new double[] { 3, 4 } } } 
                    }
                },
                EndDate = currentDate
            };

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var clonedFilter = filter.Clone();
            clonedFilter.TaxonIds = new[] {4000107};

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            clonedFilter.Should().NotBeEquivalentTo(filter);
        }
    }
}