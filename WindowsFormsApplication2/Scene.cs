using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using GlmNet;
using SharpGL;
using SharpGL.Shaders;
using SharpGL.VertexBuffers;
using IFCViewer;

namespace WindowsFormsApplication2
{
    public class Scene
    {
        // 매트릭스
        mat4 matProj;
        mat4 matView;
        mat4 matWorld;

        const uint attributeIndexPosition = 0;
        const uint attributeIndexNormal = 1;

        // 버텍스와 픽셀 쉐이더에 대한 쉐이더 프로그램
        private ShaderProgram shaderProgram;

        int counts = 0;

        // 모델 중심점
        vec3 center = new vec3(0.0f, 0.0f, 0.0f);

        float rads = 0.0f;

        float width = 0.0f;

        float height = 0.0f;


        Camera camera = Camera.Instance;


        IFCViewerWrapper ifcParser = IFCViewerWrapper.Instance;

        int indexCount = 0;
        int vertexCount = 0;


        public vec3 Center
        {
            get { return center; }
        }

        public void ClearScene()
        {
            ifcParser.ClearMemory();
        }

        public void ParseIFCFile(string sPath)
        {
            ifcParser.ParseIfcFile(sPath);
        }

        public void InitScene(OpenGL gl, float w, float h)
        {

            // 배경 클리어
            gl.ClearColor(0.4f, 0.4f, 0.4f, 1.0f);
            //// 쉐이더 프로그램 생성
            var vertexShaderSource = ShaderLoader.LoadShaderFile("Shader.vert");
            var fragmentShaderSource = ShaderLoader.LoadShaderFile("Shader.frag");
            shaderProgram = new ShaderProgram();
            shaderProgram.Create(gl, vertexShaderSource, fragmentShaderSource, null);
            shaderProgram.BindAttributeLocation(gl, attributeIndexPosition, "inputPosition");
            shaderProgram.BindAttributeLocation(gl, attributeIndexNormal, "inputNormal");
            shaderProgram.AssertValid(gl);
            
            // 원근 투영 매트릭스 생성
            rads = 0.25f * (float)Math.PI;
            matProj = glm.perspective(rads, w / h, 1.0f, 10000.0f);
            width = w;
            height = h;

            // 시야 행렬 생성
            matView = camera.LookAt(new vec3(0.0f, -4.0f, 0.0f), new vec3(0.0f, 0.0f, 0.0f), new vec3(0.0f, 0.0f, 1.0f));

            testCreateBuffer(gl);

        }

        public void testCreateBuffer(OpenGL gl)
        {
            float[] tVertex = { 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f };
            ushort[] tIndex = { 0, 1, 2 };
            ifcParser.vertexBufferArray = new VertexBufferArray();
            ifcParser.vertexBufferArray.Create(gl);
            ifcParser.vertexBufferArray.Bind(gl);

            ifcParser.vertexBuffer = new VertexBuffer();
            ifcParser.vertexBuffer.Create(gl);
            ifcParser.vertexBuffer.Bind(gl);
            ifcParser.vertexBuffer.SetData(gl, 0, tVertex, false, 3);

            ifcParser.indexBuffer = new IndexBuffer();
            ifcParser.indexBuffer.Create(gl);
            ifcParser.indexBuffer.Bind(gl);
            ifcParser.indexBuffer.SetData(gl, tIndex);

            ifcParser.vertexBufferArray.Unbind(gl);

        }

        public void InitDeviceBuffer(OpenGL gl, float width, float height)
        {
            GlmNet.vec3 min = new GlmNet.vec3();
            GlmNet.vec3 max = new GlmNet.vec3();

            bool initMinMax = false;
            GetDimensions(ref min, ref max, ref initMinMax);

            GlmNet.vec3 center = new GlmNet.vec3();
            center.x = (max.x + min.x) / 2f;
            center.y = (max.y + min.y) / 2f;
            center.z = (max.z + min.z) / 2f;

            float size = max.x - min.x;

            if (size < max.y - min.y) size = max.y - min.y;
            if (size < max.z - min.z) size = max.z - min.z;

            float thetaY = 0.25f * (float)Math.PI;
            float thetaX = 2.0f * (float)Math.Atan(width / height * (float)Math.Tan((double)thetaY * 0.5));

            // 정면부와 후면부에서 바라볼 때 x좌표를 기준으로 초점 거리를 구한다. 
            float Dx1 = camera.CalculateDistance(center.x, max.x, min.x, thetaX);
            // 측면부에서 바라볼 때 x좌표를 기준으로 초점거리를 구한다.
            float Dx2 = camera.CalculateDistance(center.z, max.z, min.z, thetaX);
            // 정면부와 후면부에서 바라볼 때 z좌표를 기준으로 초점거리를 구한다.
            float Dy1 = camera.CalculateDistance(center.z, max.z, min.z, thetaY);
            // 윗면부에서 바라볼 때 y좌표를 기준으로 초점거리를 구한다. 
            float Dy2 = camera.CalculateDistance(center.y, max.y, min.y, thetaY);

            // 가장 큰 거리를 구한다.
            float Dx = Dx1 > Dx2 ? Dx1 : Dx2;
            float Dy = Dy1 > Dy2 ? Dy1 : Dy2;

            // 카메라의 최종 초점 거리
            float cameraDistance = Dx > Dy ? Dx : Dy;
            
            matView = camera.LookAt(new vec3(center.x, center.y - cameraDistance, center.z), center, new vec3(0.0f, 0.0f, 1.0f));

            Int64 vBuffSize = 0, iBuffSize = 0;

            GetFaceBufferSize(ref vBuffSize, ref iBuffSize);

            vertexCount = (int)vBuffSize;
            indexCount = (int)iBuffSize;
          
            if (vBuffSize == 0)
                return;

            ifcParser.vertexBuffer = new VertexBuffer();
            ifcParser.indexBuffer = new IndexBuffer();
            ifcParser.vertexBufferArray = new VertexBufferArray();


            int vSize = 0;
            int iSize = 0;
            ifcParser.vertexBufferArray.Create(gl);
            ifcParser.vertexBufferArray.Bind(gl);

            ifcParser.vertexBuffer.Create(gl);
            ifcParser.vertexBuffer.Bind(gl);
            gl.BufferData(OpenGL.GL_ARRAY_BUFFER, sizeof(float) * (int)vBuffSize, IntPtr.Zero, OpenGL.GL_STATIC_DRAW);

            // 모든 객체의 버텍스 집합을 한 버퍼에 집어 넣는다.
            for (var i = 0; i < ifcParser.ifcItemList.Count; ++i)
            {
                GCHandle pinnedVertexArray = GCHandle.Alloc(ifcParser.ifcItemList[i].verticesForFaces, GCHandleType.Pinned);
                IntPtr vertexPointer = pinnedVertexArray.AddrOfPinnedObject();
                gl.BufferSubData(OpenGL.GL_ARRAY_BUFFER, vSize, sizeof(float) * (int)ifcParser.ifcItemList[i].noVerticesForFaces, vertexPointer);

                vSize += sizeof(float) * (int)ifcParser.ifcItemList[i].noVerticesForFaces;
                pinnedVertexArray.Free();
            }

            gl.VertexAttribPointer(0, 6, OpenGL.GL_FLOAT, false, 0, IntPtr.Zero);
            gl.EnableVertexAttribArray(0);

            ifcParser.indexBuffer.Create(gl);
            ifcParser.indexBuffer.Bind(gl);
            gl.BufferData(OpenGL.GL_ARRAY_BUFFER, sizeof(int) * (int)iBuffSize, IntPtr.Zero, OpenGL.GL_STATIC_DRAW);

            // 모든 객체의 인덱스 집합을 한 버퍼에 집어 넣는다.
            for (var i = 0; i < ifcParser.ifcItemList.Count; ++i)
            {
                GCHandle pinnedIndexArray = GCHandle.Alloc(ifcParser.ifcItemList[i].indicesForFaces, GCHandleType.Pinned);
                IntPtr indexPointer = pinnedIndexArray.AddrOfPinnedObject();
                gl.BufferSubData(OpenGL.GL_ARRAY_BUFFER, iSize, sizeof(int) * (int)ifcParser.ifcItemList[i].noPrimitivesForFaces, indexPointer);

                iSize += sizeof(int) * (int)ifcParser.ifcItemList[i].noPrimitivesForFaces;
                pinnedIndexArray.Free();
            }


            ifcParser.vertexBufferArray.Unbind(gl);

            gl.Enable(OpenGL.GL_CULL_FACE);
            gl.FrontFace(OpenGL.GL_CW);

            gl.Enable(OpenGL.GL_DEPTH_TEST);
            gl.DepthFunc(OpenGL.GL_LEQUAL);

        }

        private void GetDimensions(ref GlmNet.vec3 min, ref GlmNet.vec3 max, ref bool initMinMax)
        {
            for (var i = 0; i < ifcParser.ifcItemList.Count; ++i)
            {
                if (ifcParser.ifcItemList[i].noVerticesForFaces != 0)
                {
                    if (initMinMax == false)
                    {
                        min.x = ifcParser.ifcItemList[i].verticesForFaces[3 * 0 + 0];
                        min.y = ifcParser.ifcItemList[i].verticesForFaces[3 * 0 + 1];
                        min.z = ifcParser.ifcItemList[i].verticesForFaces[3 * 0 + 2];
                        max = min;

                        initMinMax = true;
                    }



                    Int64 j = 0;
                    while (j < ifcParser.ifcItemList[i].noVerticesForFaces)
                    {
                        min.x = Math.Min(min.x, ifcParser.ifcItemList[i].verticesForFaces[6 * j + 0]);
                        min.y = Math.Min(min.y, ifcParser.ifcItemList[i].verticesForFaces[6 * j + 1]);
                        min.z = Math.Min(min.z, ifcParser.ifcItemList[i].verticesForFaces[6 * j + 2]);

                        max.x = Math.Max(max.x, ifcParser.ifcItemList[i].verticesForFaces[6 * j + 0]);
                        max.y = Math.Max(max.y, ifcParser.ifcItemList[i].verticesForFaces[6 * j + 1]);
                        max.z = Math.Max(max.z, ifcParser.ifcItemList[i].verticesForFaces[6 * j + 2]);

                        ++j;
                    }
                }
            }
        }

        private void GetFaceBufferSize(ref Int64 vBuffSize, ref Int64 iBuffSize)
        {
            for (var i = 0; i < ifcParser.ifcItemList.Count; ++i)
            {
                if (ifcParser.ifcItemList[i].ifcID != 0 && ifcParser.ifcItemList[i].noVerticesForFaces != 0 && ifcParser.ifcItemList[i].noPrimitivesForFaces != 0)
                {
                    ifcParser.ifcItemList[i].vertexOffsetForFaces = vBuffSize;
                    ifcParser.ifcItemList[i].indexOffsetForFaces = iBuffSize;

                    vBuffSize += ifcParser.ifcItemList[i].noVerticesForFaces;
                    iBuffSize += 3 * ifcParser.ifcItemList[i].noPrimitivesForFaces;
                }
            }
        }

        public void CreateBuffer(OpenGL gl)
        {
            gl.Enable(OpenGL.GL_CULL_FACE);
            gl.FrontFace(OpenGL.GL_CW);

            gl.Enable(OpenGL.GL_DEPTH_TEST);
            gl.DepthFunc(OpenGL.GL_LEQUAL);
        }

        public void Render(OpenGL gl)
        {
            // 장면 클리어
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT | OpenGL.GL_STENCIL_BUFFER_BIT);

            shaderProgram.Bind(gl);
            shaderProgram.SetUniformMatrix4(gl, "matProj", matProj.to_array());
            shaderProgram.SetUniformMatrix4(gl, "matView", matView.to_array());

            //if(ifcParser.ifcItemList.Count != 0)
            //{
            //    ifcParser.vertexBufferArray.Bind(gl);

            //    gl.DrawElements(OpenGL.GL_TRIANGLES, indexCount, OpenGL.GL_UNSIGNED_SHORT, IntPtr.Zero);

            //    ifcParser.vertexBufferArray.Unbind(gl);
            //}
            gl.DrawArrays(OpenGL.GL_TRIANGLES, 0, 3);

            shaderProgram.Unbind(gl);

//          gl.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);

            
           
        }
    }
}
