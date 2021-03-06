﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace GenesisEngine
{
    public class QuadNode : IQuadNode, IDisposable
    {
        DoubleVector3 _locationRelativeToPlanet;
        double _planetRadius;
        DoubleVector3 _uVector;
        DoubleVector3 _vVector;
        DoubleVector3 _planeNormalVector;
        protected QuadNodeExtents _extents;

        bool _hasSubnodes;
        bool _splitInProgress;
        bool _mergeInProgress;
        protected Task _backgroundSplitTask;
        protected List<IQuadNode> _subnodes = new List<IQuadNode>();

        readonly IQuadMesh _mesh;
        readonly IQuadNodeFactory _quadNodeFactory;
        readonly IQuadNodeRenderer _renderer;
        readonly ISettings _settings;
        readonly Statistics _statistics;

        public QuadNode(IQuadMesh mesh, IQuadNodeFactory quadNodeFactory, IQuadNodeRenderer renderer, ISettings settings, Statistics statistics)
        {
            _mesh = mesh;
            _quadNodeFactory = quadNodeFactory;
            _renderer = renderer;
            _settings = settings;
            _statistics = statistics;
        }

        public int Level { get; private set; }

        // TODO: push this data in through the constructor, probably in a QuadNodeDefintion class, and make
        // this method private.  Except that would do real work in construction.  Hmmm.
        public void Initialize(double planetRadius, DoubleVector3 planeNormalVector, DoubleVector3 uVector, DoubleVector3 vVector, QuadNodeExtents extents, int level)
        {
            _planetRadius = planetRadius;
            _planeNormalVector = planeNormalVector;
            _uVector = uVector;
            _vVector = vVector;
            _extents = extents;
            Level = level;

            _locationRelativeToPlanet = (_planeNormalVector) + (_uVector * ((_extents.West + (_extents.Width / 2.0)))) + (_vVector * ((_extents.North + (_extents.Width / 2.0))));
            _locationRelativeToPlanet = _locationRelativeToPlanet.ProjectUnitPlaneToUnitSphere() * _planetRadius;

            _mesh.Initialize(planetRadius, planeNormalVector, uVector, vVector, extents, level);

            _statistics.NumberOfQuadNodes++;
            _statistics.NumberOfQuadNodesAtLevel[Level]++;
        }

        public void Update(DoubleVector3 cameraLocation, DoubleVector3 planetLocation, ClippingPlanes clippingPlanes)
        {
            _mesh.Update(cameraLocation, planetLocation, clippingPlanes);

            // TODO: This algorithm could be improved to optimize the number of triangles that are drawn

            if (_mesh.IsVisibleToCamera && _mesh.CameraDistanceToWidthRatio < 1 && !_hasSubnodes && !_splitInProgress && !_mergeInProgress && Level < _settings.MaximumQuadNodeLevel)
            {
                Split(cameraLocation, planetLocation, clippingPlanes);
            }
            else if (_mesh.CameraDistanceToWidthRatio > 1.2 && _hasSubnodes && !_mergeInProgress && !_splitInProgress)
            {
                Merge();
            }

            if (_hasSubnodes)
            {
                foreach (var subnode in _subnodes)
                {
                    subnode.Update(cameraLocation, planetLocation, clippingPlanes);
                }
            }
        }

        private void Split(DoubleVector3 cameraLocation, DoubleVector3 planetLocation, ClippingPlanes clippingPlanes)
        {
            _splitInProgress = true;
            var subextents = _extents.Split();

            // TODO: refactor
            // TODO: should we bother to write specs for the threading behavior?

            var tasks = new List<Task<IQuadNode>>();
            foreach (var subextent in subextents)
            {
                var capturedExtent = subextent;
                var task = Task<IQuadNode>.Factory.StartNew(() =>
                {
                    var node = _quadNodeFactory.Create();
                    node.Initialize(_planetRadius, _planeNormalVector, _uVector, _vVector, capturedExtent, Level + 1);
                    node.Update(cameraLocation, planetLocation, clippingPlanes);
                    return node;
                });

                tasks.Add(task);
            }

            _backgroundSplitTask = Task.Factory.ContinueWhenAll(tasks.ToArray(), finishedTasks =>
            {
                foreach (var task in finishedTasks)
                {
                    _subnodes.Add(task.Result);
                }

                _hasSubnodes = true;
                _splitInProgress = false;
            });
        }

        private void Merge()
        {
            // TODO: Do async disposal
            // TODO: if a split is pending, cancel it
            _mergeInProgress = true;
            _hasSubnodes = false;

            Task.Factory.StartNew(() =>
            {
                DisposeSubNodes();
                _subnodes.Clear();

                _mergeInProgress = false;
            });
        }

        void DisposeSubNodes()
        {
            foreach (var node in _subnodes)
            {
                ((IDisposable)node).Dispose();
            }
        }

        public void Draw(DoubleVector3 cameraLocation, Matrix originBasedViewMatrix, Matrix projectionMatrix)
        {
            if (!_mesh.IsVisibleToCamera)
            {
                return;
            }

            if (_hasSubnodes)
            {
                foreach (var subnode in _subnodes)
                {
                    subnode.Draw(cameraLocation, originBasedViewMatrix, projectionMatrix);
                }
            }
            else
            {
                _renderer.Draw(_locationRelativeToPlanet, cameraLocation, originBasedViewMatrix, projectionMatrix);
                _mesh.Draw(cameraLocation, originBasedViewMatrix, projectionMatrix);
            }
        }

        public void Dispose()
        {
            ((IDisposable)_renderer).Dispose();
            DisposeSubNodes();

            _statistics.NumberOfQuadNodes--;
            _statistics.NumberOfQuadNodesAtLevel[Level]--;
        }
    }
}

