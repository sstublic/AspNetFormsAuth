﻿/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhetos.Utilities;

namespace Rhetos.Extensibility
{
    public class GeneratorPlugins
    {
        private IEnumerable<IGenerator> _generators;

        public GeneratorPlugins(IEnumerable<IGenerator> generators)
        {
            this._generators = generators;
        }

        public IList<IGenerator> GetGenerators()
        {
            var genNames = _generators.Select(gen => gen.GetType().FullName).ToList();
            var genDependencies = _generators.SelectMany(gen => (gen.Dependencies ?? new string[0]).Select(x => Tuple.Create(x, gen.GetType().FullName)));
            Rhetos.Utilities.Graph.TopologicalSort(genNames, genDependencies);

            var sortedGenerators = _generators.ToArray();
            Graph.SortByGivenOrder(sortedGenerators, genNames.ToArray(), gen => gen.GetType().FullName);
            return sortedGenerators;
        }
    }
}