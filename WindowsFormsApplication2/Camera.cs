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
        private float moveFactor = 1.0f;

        // 카메라가 바라보는 목표
        private vec3 pCenter;

        private float nearDepth = 1.0f;

        private float farDepth = 10.0f;


        #region 싱글톤
        private static Camera instance = new Camera();

        private Camera() { }

        public static Camera Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion

        #region 프로퍼티
        public mat4 MatView
        {
            get { return matView; }
        }

        public mat4 MatProj
        {
            get { return matProj; }
        }

        public vec3 Look
        {
            get { return vLook; }
        }

        public vec3 Center
        {
            set { pCenter = value; }
            get { return pCenter; }
        }

        public float NearDepth
        {
            set { nearDepth = value; }
            get { return nearDepth; }
        }

        public float FarDepth
        {
            set { farDepth = value; }
            get { return farDepth; }
        }

        public float CameraDistance
        {
            set { cameraDistance = value; }
            get { return cameraDistance; }
        }
        #endregion

        private void CalculateMoveFactor()
        {
            vec3 temp = pCenter - pEye;

            float currentDistance = (float)Math.Sqrt((double)VecMath.vec3Dot(temp, temp));

            if (currentDistance / cameraDistance < 0.005f) currentDistance = 0.005f * cameraDistance;

            moveFactor = 0.4f * currentDistance;
        }


        // 카메라 파라미터 및 뷰 행렬 생성
        public mat4 LookAt(vec3 eye, vec3 target, vec3 u)
        {
            vLook = glm.normalize(eye - target);
            vUp = glm.normalize(u);
            vSide = glm.normalize(glm.cross(vUp, vLook));
            vUp = glm.cross(vLook, vSide);

            pEye = eye;

            eye = new vec3(-VecMath.vec3Dot(vSide, eye), -VecMath.vec3Dot(vUp, eye), -VecMath.vec3Dot(vLook, eye));

            vec4[] temp = new vec4[4];
            temp[0] = new vec4(vSide.x, vUp.x, vLook.x, 0.0f);
            temp[1] = new vec4(vSide.y, vUp.y, vLook.y, 0.0f);
            temp[2] = new vec4(vSide.z, vUp.z, vLook.z, 0.0f);
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

            vec4 p = new vec4(-VecMath.vec3Dot(vSide, pEye), -VecMath.vec3Dot(vUp, pEye), -VecMath.vec3Dot(vLook, pEye), 1.0f);

            vec4[] temp = new vec4[4];

            temp[0] = new vec4(vSide[0], vUp[0], vLook[0], 0.0f);
            temp[1] = new vec4(vSide[1], vUp[1], vLook[1], 0.0f);
            temp[2] = new vec4(vSide[2], vUp[2], vLook[2], 0.0f);
            temp[3] = p;

            mat4 m = new mat4(temp);

            matView = m;

        }

        // 좌우 이동
        public void Strafe(float d)
        {
            CalculateMoveFactor();

            pEye += - 0.007f * moveFactor * d * vSide;

            pCenter += - 0.007f * moveFactor * d * vSide;
        }

        // 상하 이동
        public void Zump(float d)
        {
            CalculateMoveFactor();

            pEye += 0.007f * moveFactor * d * vUp;

            pCenter += 0.007f * moveFactor * d * vUp;
        }

        // 앞뒤 이동
        public void Walk(float d)
        {
            CalculateMoveFactor();

            vec3 prevHead = pCenter - pEye;
            prevHead = glm.normalize(prevHead);

            pEye += -1.0f * moveFactor * d * prevHead;

            vec3 currHead = pCenter - pEye;
            currHead = glm.normalize(currHead);

            if(VecMath.vec3Dot(prevHead, currHead) < 0.9999)
            {
                pCenter = -0.01f * cameraDistance * vLook + pCenter;
            }

        }

        public void Yaw(float angle)
        {
            mat4 rot = glm.rotate(new mat4(1.0f), angle, new vec3(0.0f, 0.0f, 1.0f));

            VecMath.vec3TransformNormal(ref vSide, ref rot);

            vLook = glm.cross(vSide, vUp);

            vUp = glm.cross(vLook, vSide);
            
        }

        public void Pitch(float angle)
        {
            mat4 rot = glm.rotate(new mat4(1.0f), angle, new vec3(1.0f, 0.0f, 0.0f));

            VecMath.vec3TransformNormal(ref vUp, ref rot);

            vLook = glm.cross(vSide, vUp);

            vSide = glm.cross(vUp, vLook);
        }

        public void OrbitUp(float angle)
        {

            pEye -= pCenter;

            vec3 y = new vec3(0.0f, 0.0f, 1.0f);

            mat4 r = glm.rotate(new mat4(1.0f), angle, y);

            VecMath.vec3TransformCoord(ref pEye, ref r);

            pEye += pCenter;

            vLook = pEye - pCenter;
            vLook = glm.normalize(vLook);

            VecMath.vec3TransformNormal(ref vSide, ref r);
            vSide = glm.normalize(vSide);

            vUp = glm.cross(vLook, vSide);
            
        }

        public void OrbitSide(float angle)
        {
            pEye -= pCenter;

            mat4 r = glm.rotate(new mat4(1.0f), angle, vSide);

            VecMath.vec3TransformCoord(ref pEye, ref r);

            pEye += pCenter;

            vLook = pEye - pCenter;
            vLook = glm.normalize(vLook);

            VecMath.vec3TransformNormal(ref vUp, ref r);
            vUp = glm.normalize(vUp);

            vSide = glm.cross(vUp, vLook);

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
