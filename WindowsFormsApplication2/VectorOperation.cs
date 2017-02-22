using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlmNet;

namespace IFCViewer
{
    // 수학 클래스는 모든 클래스에서 사용할 수 있는 전역 함수를 정의
    class VecMath
    {

        // 내적
        public static float vec3Dot(vec3 a, vec3 b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        // 벡터 변환
        public static void vec3TransformNormal(ref vec3 vec, ref mat4 m)
        {
            vec3 temp = new vec3(m[0, 0] * vec.x + m[1, 0] * vec.y + m[2, 0] * vec.z,
                                 m[0, 1] * vec.x + m[1, 1] * vec.y + m[2, 1] * vec.z,
                                 m[0, 2] * vec.x + m[1, 2] * vec.y + m[2, 2] * vec.z);


            vec = glm.normalize(temp);
        }

        // 위치 변환
        public static void vec3TransformCoord(ref vec3 pos, ref mat4 m)
        {
            vec3 temp = new vec3(m[0, 0] * pos.x + m[1, 0] * pos.y + m[2, 0] * pos.z + m[3, 0] * 1.0f,
                                m[0, 1] * pos.x + m[1, 1] * pos.y + m[2, 1] * pos.z + m[3, 1] * 1.0f,
                                m[0, 2] * pos.x + m[1, 2] * pos.y + m[2, 2] * pos.z + m[3, 2] * 1.0f);

            pos = temp;
        }
       
    }
}
