using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlmNet;

namespace IFCViewer
{
    class VecMath
    {

        public static float vec3Dot(vec3 a, vec3 b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        public static void vec3TransformNormal(ref vec3 vec, ref mat4 m)
        {
            vec3 temp = new vec3(m[0, 0] * vec.x + m[1, 0] * vec.y + m[2, 0] * vec.z,
                                 m[0, 1] * vec.x + m[1, 1] * vec.y + m[2, 1] * vec.z,
                                 m[0, 2] * vec.x + m[1, 2] * vec.y + m[2, 2] * vec.z);


            vec = glm.normalize(temp);
        }




        public static void vec3TransformCoord(ref vec3 pos, ref mat4 m)
        {
            vec3 temp = new vec3(m[0, 0] * pos.x + m[1, 0] * pos.y + m[2, 0] * pos.z + m[3, 0] * 1.0f,
                                m[0, 1] * pos.x + m[1, 1] * pos.y + m[2, 1] * pos.z + m[3, 1] * 1.0f,
                                m[0, 2] * pos.x + m[1, 2] * pos.y + m[2, 2] * pos.z + m[3, 2] * 1.0f);

            pos = temp;
        }
    }
}
