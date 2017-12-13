// <copyright file="GlobalSuppressions.cs" company="Kevin Locke">
// Copyright 2017 Kevin Locke &lt;kevin@kevinlocke.name&gt;
// This file is part of KevinLocke.VisualStudio.GlobalDataSetGenerators,
// publicly available under the MIT License.  See LICENSE.txt for details.
// </copyright>

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the
// Code Analysis results, point to "Suppress Message", and click
// "In Suppression File".
// You do not need to add suppressions to this file manually.
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Microsoft.Design",
    "CA2210:AssembliesShouldHaveValidStrongNames",
    Justification = "Disabled until I work out a way to sign securely on CI servers")]