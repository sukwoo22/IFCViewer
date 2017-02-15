using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlmNet;

namespace IFCViewer
{
    class Camera
    {
        // 카메라 위치
        private vec3 pEye;

        // 카메라 up 벡터
        private vec3 vUp;

        // 카메라 side 벡터
        private vec3 vSide;

        // 카메라 look 벡터
        private vec3 vLook;

        // 시야 행렬
        private mat4 matView;

        // 투영 행렬
        private mat4 matProj;

        // 카메라 위치와 바라보는 위치 사이의 거리
        private float cameraDistance = 1.0f;
       
        // 이동 계수
        private float moveCoef = 1.0f;

        // 모델 중심점
        private vec3 pCenter;

        // 전체 모델의 최소 위치
        private vec3 pMin;

        // 전체 모델의 최대 위치
        private vec3 pMax;

        private void calculateCameraDistance(vec3 posMax, vec3 posMin)
        {
            cameraDistance = 0.33334f * (posMax.x - posMin.x + posMax.y - posMin.y + posMax.z - posMax.z);
        }

        private void updateCoef(ref vec3 center)
        {
            vec3 temp = center - pEye;

            moveCoef = Math.Abs(temp.x) + Math.Abs(temp.y) + Math.Abs(temp.z);

            moveCoef = moveCoef < 1.0f ? 1.0f : moveCoef;
        }

        private static Camera instance = new Camera();

        private Camera() { }

        public static Camera Instance
        {
            get
            {
                return instance;
            }
        }

        public mat4 MatView
        {
            get { return matView; }
        }

        public mat4 MatProj
        {
            get { return matProj; }
        }

        public vec3 PosMin
        {
            get { return pMin; }
        }

        public vec3 PosMax
        {
            get { return pMax; }
        }


        private float vec3Dot(vec3 a, vec3 b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        private void vec3TransformNormal(ref vec3 vec, ref mat4 m)
        {
            vec3 temp = new vec3(m[0, 0] * vec.x + m[1, 0] * vec.y + m[2, 0] * vec.z,
                                 m[0, 1] * vec.x + m[1, 1] * vec.y + m[2, 1] * vec.z,
                                 m[0, 2] * vec.x + m[1, 2] * vec.y + m[2, 2] * vec.z);

            
            vec = glm.normalize(temp);
        }

        //vec3 vec3TransformNormal(ref vec3 vec, ref mat4 m)
        //{
        //    vec3 temp = new vec3(m[0, 0] * vec.x + m[1, 0] * vec.y + m[2, 0] * vec.z,
        //                        m[0, 1] * vec.x + m[1, 1] * vec.y + m[2, 1] * vec.z,
        //                        m[0, 2] * vec.x + m[1, 2] * vec.y + m[2, 2] * vec.z);


        //    return glm.normalize(temp);
        //}

        private void vec3TransformCoord(ref vec3 pos, ref mat4 m)
        {
            vec3 temp = new vec3(m[0, 0] * pos.x + m[1, 0] * pos.y + m[2, 0] * pos.z + m[3, 0] * 1.0f,
                                m[0, 1] * pos.x + m[1, 1] * pos.y + m[2, 1] * pos.z + m[3, 1] * 1.0f,
                                m[0, 2] * pos.x + m[1, 2] * pos.y + m[2, 2] * pos.z + m[3, 2] * 1.0f);

            pos = temp;
        }

        // 카메라 파라미터 및 뷰 행렬 생성
        public mat4 LookAt(vec3 eye, vec3 target, vec3 u)
        {
            vLook = glm.normalize(eye - target);
            vUp = glm.normalize(u);
            vSide = glm.normalize(glm.cross(vUp, vLook));
            vUp = glm.cross(vLook, vSide);

            pEye = eye;

            eye = new vec3(-vec3Dot(vSide, eye), -vec3Dot(vUp, eye), -vec3Dot(vLook, eye));

            vec4[] temp = new vec4[4];
            temp[0] = new vec4(vSide.x, vUp.x, vLook[0], 0.0f);
            temp[1] = new vec4(vSide.y, vUp.y, vLook[1], 0.0f);
            temp[2] = new vec4(vSide.z, vUp.z, vLook[2], 0.0f);
            temp[3] = new vec4(eye.x, eye.y, eye.z, 1.0f);

            matView = new mat4(temp);

            return matView;
        }


        // 원근 투영 행렬 생성
        public mat4 Perspective(float fovY, float aspect, float n, float f)
        {
            float q = 1.0f / (float)Math.Tan(0.5f * (double)fovY);

            float A = q / aspect;

            float B = (n + f) / (n - f);

            float C = (2.0f * n * f) / (n - f);

            matProj = new mat4(1.0f);

            matProj[0] = new vec4(A, 0.0f, 0.0f, 0.0f);
            matProj[1] = new vec4(0.0f, q, 0.0f, 0.0f);
            matProj[2] = new vec4(0.0f, 0.0f, B, -1.0f);
            matProj[3] = new vec4(0.0f, 0.0f, C, 0.0f);

            return matProj;
        }

        // 뷰 행렬 업데이트
        public void UpdateViewMatrix()
        {
            // look 벡터
            vLook = glm.normalize(vLook);

            // up 벡터
            vUp = glm.normalize(vUp);

            // side 벡터
            vSide = glm.normalize(vSide);

            
            vec4 p = new vec4(-vec3Dot(vSide, pEye), -vec3Dot(vUp, pEye), -vec3Dot(vLook, pEye), 1.0f);

            vec4[] temp = new vec4[4];

            temp[0] = new vec4(vSide[0], vUp[0], vLook[0], 0.0f);
            temp[1] = new vec4(vSide[1], vUp[1], vLook[1], 0.0f);
            temp[2] = new vec4(vSide[2], vUp[2], vLook[2], 0.0f);
            temp[3] = p;

            mat4 m = new mat4(temp);

            matView = m;

        }


        public void Strafe(float d)
        {
            pEye += -0.3f * moveCoef * d * vSide;
        }

        public void Zump(float d)
        {
            pEye += 0.3f * moveCoef * d * vUp;
        }

        public void Walk(float d)
        {
            pEye += 1.0f * cameraDistance * d * vLook;
        }

        public void Yaw(float angle)
        {
            mat4 rot = glm.rotate(new mat4(1.0f), angle, new vec3(0.0f, 0.0f, 1.0f));

            vec3TransformNormal(ref vSide, ref rot);

            vLook = glm.cross(vSide, vUp);

            vUp = glm.cross(vLook, vSide);
            
        }

        public void Pitch(float angle)
        {
            mat4 rot = glm.rotate(new mat4(1.0f), angle, new vec3(1.0f, 0.0f, 0.0f));

            vec3TransformNormal(ref vUp, ref rot);

            vLook = glm.cross(vSide, vUp);

            vSide = glm.cross(vUp, vLook);
        }

        public void ObitY(float angle, vec3 center)
        {


        }

        public void FitCamera()
        {

        }

       

        public float CalculateDistance(float center, float max, float min, float theta)
        {
            float offset = 0.0f;

            // 센터를 기준으로 가장 멀리 떨어진 거리
            float maxDistance = max - center + offset;


            float d = maxDistance / (float)Math.Tan(0.5 * (double)theta);

            return d;
        }

    }

}
