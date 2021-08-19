using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapArrow : MonoBehaviour
{
    [SerializeField]
    SkinnedMeshRenderer _renderer = null;

    public List<Vector2> points = null;

    // Start is called before the first frame update
    void Start()
    {
        _renderer.sortingLayerName = _sortingLayerName;
    }

    [SerializeField]
    string _sortingLayerName = null;

    // Update is called once per frame
    void Update()
    {
    }


    public void Recalculate()
    {
        _renderer.sortingLayerName = _sortingLayerName;

        float arrow_head_length = 0.4f;
        float arrow_head_width = 1f;
        float arrow_width_base = 0.6f;
        float arrow_width = 0.4f;
        int num_segments = 32;

        float curveLength = 0.5f;

        List<Vector2> path = new List<Vector2>();
        for(int i = 0; i < points.Count-1; ++i) {
            for(int j = 0; j < num_segments; ++j) {

                Vector2[] p = null;
                float t = 0f;

                float x = (float)j/(float)num_segments;
                if(x < curveLength && i > 0) {
                    p = new Vector2[] {
                        Vector2.Lerp(points[i-1], points[i], 1f-curveLength),
                        points[i],
                        Vector2.Lerp(points[i], points[i+1], curveLength),
                    };

                    t = 0.5f + 0.5f*x/curveLength;
                } else if(x > 1f - curveLength && i+2 < points.Count) {
                    p = new Vector2[] {
                        Vector2.Lerp(points[i], points[i+1], 1f-curveLength),
                        points[i+1],
                        Vector2.Lerp(points[i+1], points[i+2], curveLength),
                    };

                    t = 0.5f*(x - (1f - curveLength))/curveLength;
                }

                if(p != null) {
                    float inv_t = 1f - t;
                    float xpos = inv_t*inv_t*p[0].x + 2*inv_t*t*p[1].x + t*t*p[2].x;
                    float ypos = inv_t*inv_t*p[0].y + 2*inv_t*t*p[1].y + t*t*p[2].y;
                    path.Add(new Vector2(xpos, ypos));

                } else {

                    float r = ((float)j)/((float)num_segments);

                    Vector2 linearPoint = Vector2.Lerp(points[i], points[i+1], r);
                    path.Add(linearPoint);
                }
            }
        }

        List<Vector2> dirs = new List<Vector2>();
        for(int i = 0; i < path.Count-1; ++i) {
            dirs.Add((path[i+1] - path[i]).normalized);
        }

        dirs.Add(dirs[dirs.Count-1]);

        Color[] colors = new Color[path.Count*2];


        List<Vector2> verts = new List<Vector2>();
        for(int i = 0; i < path.Count; ++i) {
            float w = Mathf.Lerp(arrow_width_base, arrow_width, i/(float)path.Count);

            if(i > path.Count - num_segments*arrow_head_length) {
                float head_ratio = (i - (path.Count - num_segments*arrow_head_length)) / (num_segments*arrow_head_length);
                w = Mathf.Lerp(arrow_head_width, 0f, head_ratio);
            }

            verts.Add(path[i] + new Vector2(dirs[i].y, -dirs[i].x)*w*0.5f);
            verts.Add(path[i] + new Vector2(-dirs[i].y, dirs[i].x)*w*0.5f);

            colors[i*2] = new Color(w, 0f, 0f, 0f);
            colors[i*2+1] = new Color(w, 1f, 0f, 0f);
        }

        Vector3[] vertices = new Vector3[verts.Count];
        Vector3[] normals = new Vector3[verts.Count];

        for(int i = 0; i != verts.Count; ++i) {
            vertices[i] = new Vector3(verts[i].x, verts[i].y, 0f);
            normals[i] = Vector3.forward;
        }


        int[] tri = new int[(verts.Count-2)*3];
        for(int i = 0; i < verts.Count-2; ++i) {
            int index = i*3;
            tri[index++] = i;

            if(i%2 == 0) {
                tri[index++] = i+1;
                tri[index++] = i+2;
            } else {
                tri[index++] = i+2;
                tri[index++] = i+1;
            }
        }

        Vector3 minpoint = vertices[0];
        Vector3 maxpoint = vertices[0];

        foreach(Vector3 v in vertices) {
            minpoint = Vector3.Min(minpoint, v);
            maxpoint = Vector3.Max(maxpoint, v);
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = tri;
        mesh.colors = colors;
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one*1000000f);

        _renderer.sharedMesh = mesh;

        _renderer.allowOcclusionWhenDynamic = false;
    }
}
