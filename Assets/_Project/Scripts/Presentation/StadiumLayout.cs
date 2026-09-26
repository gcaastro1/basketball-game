using System;
using System.Collections.Generic;
using UnityEngine;

namespace Basket.Presentation
{
    // Where each piece of a stadium goes, relative to the center of its court at floor level
    // (x across, z along the court). Generated from the asset's demo scene by
    // tools/stadium/extract_layout.py; the game builds it at runtime (ArenaDresser).
    [CreateAssetMenu(fileName = "StadiumLayout", menuName = "Basket/Stadium Layout")]
    public class StadiumLayout : ScriptableObject
    {
        [Serializable]
        public struct Piece
        {
            public GameObject prefab;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            // Material slots replaced on the piece's renderer (empty = the prefab's own).
            public Material[] materials;
        }

        // Size of the court drawn on the floor (for checks and docs).
        public Vector2 courtSize = new Vector2(15.24f, 28.65f);
        public List<Piece> pieces = new List<Piece>();
    }
}
