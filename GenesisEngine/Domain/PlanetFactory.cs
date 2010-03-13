﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StructureMap;

namespace GenesisEngine
{
    public class PlanetFactory : IPlanetFactory
    {
        readonly ITerrainFactory _terrainFactory;

        public PlanetFactory(ITerrainFactory terrainFactory)
        {
            _terrainFactory = terrainFactory;
        }

        public IPlanet Create(DoubleVector3 location, double radius)
        {
            var terrain = _terrainFactory.Create(radius);
            var renderer = CreateRenderer(radius);
            var generator = ObjectFactory.GetInstance<IHeightfieldGenerator>();
            var statistics = ObjectFactory.GetInstance<Statistics>();

            var planet = new Planet(location, radius, terrain, renderer, generator, statistics);

            return planet;
        }

        IPlanetRenderer CreateRenderer(double radius)
        {
            var contentManager = ObjectFactory.GetInstance<ContentManager>();
            var graphicsDevice = ObjectFactory.GetInstance<GraphicsDevice>();

            return new PlanetRenderer(radius, contentManager, graphicsDevice);
        }
    }
}
