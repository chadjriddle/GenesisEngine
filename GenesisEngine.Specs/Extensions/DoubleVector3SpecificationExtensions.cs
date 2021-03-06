﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Machine.Specifications;

namespace GenesisEngine.Specs
{
    /// <summary>
    /// Provides BDD-style assertations for DoubleVector3.
    /// </summary>
    public static class DoubleVector3SpecificationExtensions
    {
        /// <summary>
        /// Verifies that the value is close to a specified value, plus or minus a allowed range.
        /// </summary>
        /// <param name="value">the value to be inspected</param>
        /// <param name="targetValue">the target value</param>
        public static void ShouldBeCloseTo(this DoubleVector3 value, DoubleVector3 targetValue)
        {
            value.ShouldBeCloseTo(targetValue, 0.000001f);
        }

        /// <summary>
        /// Verifies that the value is close to a specified value, plus or minus a allowed range.
        /// </summary>
        /// <param name="value">the value to be inspected</param>
        /// <param name="targetValue">the target value</param>
        /// <param name="allowableDelta">the allowable plus-or-minus range</param>
        public static void ShouldBeCloseTo(this DoubleVector3 value, DoubleVector3 targetValue, Single allowableDelta)
        {
            value.X.ShouldBeCloseTo(targetValue.X, allowableDelta);
            value.Y.ShouldBeCloseTo(targetValue.Y, allowableDelta);
            value.Z.ShouldBeCloseTo(targetValue.Z, allowableDelta);
        }
    }
}
