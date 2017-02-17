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

namespace IFCViewer
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

        float NEARDEPTH = 1.0f;
        float FARDEPTH = 10000.0f;

        // 조명
        struct LIGHT
        {
            public vec4 diffuse;
            public vec4 specular;
            public vec4 ambient;
            public vec3 position;
            public vec3 direction;
            public float range;
        }

        LIGHT light1 = new LIGHT();
        LIGHT light2 = new LIGHT();
        LIGHT light3 = new LIGHT();


        Camera camera = Camera.Instance;
        IFCViewerWrapper ifcParser = IFCViewerWrapper.Instance;
        List<IFCItem> modelList = new List<IFCItem>();

        int indexCount = 0;
        int vertexCount = 0;


        public vec3 Center
        {
            get { return center; }
        }


        private void AddModel(int startIndex)
        {
           for(var i= startIndex; i < ifcParser.ifcItemList.Count; ++i)
           {
               if(ifcParser.ifcItemList[i].noVerticesForFaces > 0)
               {
                   modelList.Add(ifcParser.ifcItemList[i]);
               }
           }
        }

        public void ClearScene(OpenGL gl)
        {
            modelList.Clear();
            ifcParser.ClearMemory();
            ifcParser.vertexBuffer.Unbind(gl);
            ifcParser.indexBuffer.Unbind(gl);
            ifcParser.vertexBufferArray.Unbind(gl);

        }

        public void ParseIFCFile(string sPath, OpenGL gl)
        {
            modelList.Clear();           
            ifcParser.ParseIfcFile(sPath);
            AddModel(0);
            InitDeviceBuffer(gl, width, height, 0);
        }

        public void AppendIFCFile(string sPath, OpenGL gl)
        {
            int itemStartIndex = ifcParser.ifcItemList.Count;
            int modelStartIndex = modelList.Count;
            ifcParser.AppendFile(sPath);
            AddModel(itemStartIndex);
            InitDeviceBuffer(gl, width, height, modelStartIndex);
        }

        public void InitScene(OpenGL gl, float w, float h)
        {

            // 배경 클리어
            gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            // 쉐이더 프로그램 생성
            var vertexShaderSource = ShaderLoader.LoadShaderFile("Shader.vert");
            var fragmentShaderSource = ShaderLoader.LoadShaderFile("Shader.frag");
            shaderProgram = new ShaderProgram();
            shaderProgram.Create(gl, vertexShaderSource, fragmentShaderSource, null);
            shaderProgram.BindAttributeLocation(gl, attributeIndexPosition, "inputPosition");
            shaderProgram.BindAttributeLocation(gl, attributeIndexNormal, "inputNormal");

            shaderProgram.AssertValid(gl);
            
            // 원근 투영 매트릭스 생성
            rads = 0.25f * (float)Math.PI;
            matProj =camera.Perspective(rads, w / h, 1.0f, 1000000.0f);
            width = w;
            height = h;


            // 시야 행렬 생성
            matView = camera.LookAt(new vec3(0.0f, 5.0f, 0.0f), new vec3(0.0f, 0.0f, 0.0f), new vec3(0.0f, 0.0f, 1.0f));

            //testCreateBuffer(gl);
            ifcParser.vertexBufferArray.Create(gl);
            ifcParser.vertexBuffer.Create(gl);
            ifcParser.indexBuffer.Create(gl);
            SetupLights(gl);


            gl.Enable(OpenGL.GL_CULL_FACE);
            gl.FrontFace(OpenGL.GL_CW);

            gl.Enable(OpenGL.GL_DEPTH_TEST);
            gl.DepthFunc(OpenGL.GL_LESS);

        }
        
        public void resize(float w, float h)
        {
            width = w;
            height = h;

            matProj = camera.Perspective(0.25f * (float)Math.PI, width / height, NEARDEPTH, FARDEPTH);
        }

        int[] indices = {
                            0, 1, 2,
                        };
        int[] indices2 = {
                            2, 3, 0,
                         };

        private void testCreateBuffer(OpenGL gl)
        {
            // 버텍스 배열
            float[] vertices = {//     위치        ||      노말
                                   0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f,
                                   1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f,
                                   1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f,
                               };

            float[] vertices2 = {//     위치        ||      노말
                                    0.0f, 0.0f, 1.0f, 1.0f, 1.0f, 1.0f,
                                };



            // 인덱스 배열
           



            
            // 버텍스 버퍼 배열 객체 생성
            ifcParser.vertexBufferArray = new VertexBufferArray();
            ifcParser.vertexBufferArray.Create(gl);
            ifcParser.vertexBufferArray.Bind(gl);

            //  버텍스 버퍼 생성
            
            var vertexDataBuffer = new VertexBuffer();
            vertexDataBuffer.Create(gl);
            vertexDataBuffer.Bind(gl);

            if (true)
            {
                IntPtr vertexPointer1 = GCHandle.Alloc(vertices, GCHandleType.Pinned).AddrOfPinnedObject();
                IntPtr vertexPointer2 = GCHandle.Alloc(vertices2, GCHandleType.Pinned).AddrOfPinnedObject();
                gl.BufferData(OpenGL.GL_ARRAY_BUFFER, sizeof(float) * (vertices.Length + vertices2.Length), IntPtr.Zero, OpenGL.GL_STATIC_DRAW);
                gl.BufferSubData(OpenGL.GL_ARRAY_BUFFER, 0, sizeof(float) * vertices.Length, vertexPointer1);
                gl.BufferSubData(OpenGL.GL_ARRAY_BUFFER, sizeof(float) * vertices.Length, sizeof(float) * vertices2.Length, vertexPointer2);
            }

            gl.VertexAttribPointer(attributeIndexPosition, 3, OpenGL.GL_FLOAT, false, sizeof(float) * 6, IntPtr.Zero);
            gl.EnableVertexAttribArray(attributeIndexPosition);
            gl.VertexAttribPointer(attributeIndexNormal, 3, OpenGL.GL_FLOAT, false, sizeof(float) * 6, IntPtr.Add(IntPtr.Zero, sizeof(float) * 3));
            gl.EnableVertexAttribArray(attributeIndexNormal);

            // 인덱스 버퍼 생성
           
            var indexDataBuffer = new IndexBuffer();
            indexDataBuffer.Create(gl);
            indexDataBuffer.Bind(gl);

            if (true)
            {
                IntPtr indexPointer1 = GCHandle.Alloc(indices, GCHandleType.Pinned).AddrOfPinnedObject();
                IntPtr indexPointer2 = GCHandle.Alloc(indices2, GCHandleType.Pinned).AddrOfPinnedObject();
                gl.BufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER, sizeof(int) * (indices.Length + indices2.Length), IntPtr.Zero, OpenGL.GL_STATIC_DRAW);
                gl.BufferSubData(OpenGL.GL_ELEMENT_ARRAY_BUFFER, 0, sizeof(int) * indices.Length, indexPointer1);
                gl.BufferSubData(OpenGL.GL_ELEMENT_ARRAY_BUFFER, sizeof(int) * indices.Length, sizeof(int) * indices2.Length, indexPointer2);
            }

            ifcParser.vertexBufferArray.Unbind(gl);

        }

        public void InitDeviceBuffer(OpenGL gl, float width, float height, int startIndex)
        {
            #region 초점 거리 계산
            vec3 min = new vec3();
            vec3 max = new vec3();

            bool initMinMax = false;
            GetDimensions(ref min, ref max, ref initMinMax);

            vec3 center = new vec3();
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
            cameraDistance *= 1.5f;

            matView = camera.LookAt(new vec3(center.x, center.y - cameraDistance, center.z), center, new vec3(0.0f, 0.0f, 1.0f));
            
            // 프러스텀의 최대 깊이를 구한다.
            float maxZ = 1.0f;

            while(cameraDistance * 2.0f > maxZ)
            {
                maxZ *= 10.0f;
            }

            maxZ *= 10000.0f;

            FARDEPTH = maxZ;

            matProj = camera.Perspective(0.25f * (float)Math.PI, width / height, NEARDEPTH, FARDEPTH);
            #endregion

            Int64 vBuffSize = 0, iBuffSize = 0;

            GetFaceBufferSize(ref vBuffSize, ref iBuffSize, startIndex);

            vertexCount = (int)vBuffSize * 6;
            indexCount = (int)iBuffSize;
          
            if (vBuffSize == 0)
                return;

            int vSize = 0;
            int iSize = 0;
            ifcParser.vertexBufferArray.Bind(gl);

            ifcParser.vertexBuffer.Bind(gl);
            gl.BufferData(OpenGL.GL_ARRAY_BUFFER, sizeof(float) * (int)vBuffSize * 6, IntPtr.Zero, OpenGL.GL_STATIC_DRAW);

            // 모든 객체의 버텍스 집합을 한 버퍼에 집어 넣는다.
            for (var i = 0; i < modelList.Count; ++i)
            {
                GCHandle pinnedVertexArray = GCHandle.Alloc(modelList[i].verticesForFaces, GCHandleType.Pinned);
                IntPtr vertexPointer = pinnedVertexArray.AddrOfPinnedObject();
                gl.BufferSubData(OpenGL.GL_ARRAY_BUFFER, vSize, sizeof(float) * (int)modelList[i].verticesForFaces.Length, vertexPointer);
                vSize += sizeof(float) * (int)modelList[i].verticesForFaces.Length;
                pinnedVertexArray.Free();
            }

            gl.VertexAttribPointer(0, 3, OpenGL.GL_FLOAT, false, sizeof(float) * 6, IntPtr.Zero);
            gl.EnableVertexAttribArray(attributeIndexPosition);
            gl.VertexAttribPointer(1, 3, OpenGL.GL_FLOAT, false, sizeof(float) * 6, IntPtr.Add(IntPtr.Zero, sizeof(float) * 3));
            gl.EnableVertexAttribArray(attributeIndexNormal);

            ifcParser.indexBuffer.Bind(gl);
            gl.BufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER, sizeof(int) * (int)iBuffSize, IntPtr.Zero, OpenGL.GL_STATIC_DRAW);

            // 모든 객체의 인덱스 집합을 한 버퍼에 집어 넣는다.
            for (var i = 0; i < modelList.Count; ++i)
            {
                GCHandle pinnedIndexArray = GCHandle.Alloc(modelList[i].indicesForFaces, GCHandleType.Pinned);
                IntPtr indexPointer = pinnedIndexArray.AddrOfPinnedObject();
                gl.BufferSubData(OpenGL.GL_ELEMENT_ARRAY_BUFFER, iSize, sizeof(int) * (int)modelList[i].indicesForFaces.Length, indexPointer);
                iSize += sizeof(int) * (int)modelList[i].indicesForFaces.Length;
                pinnedIndexArray.Free();
            }

            ifcParser.vertexBufferArray.Unbind(gl);
            
        }

        private void GetDimensions(ref GlmNet.vec3 min, ref GlmNet.vec3 max, ref bool initMinMax)
        {
            for (var i = 0; i < modelList.Count; ++i)
            {
                if (modelList[i].noVerticesForFaces != 0)
                {
                    if (initMinMax == false)
                    {
                        min.x = modelList[i].verticesForFaces[3 * 0 + 0];
                        min.y = modelList[i].verticesForFaces[3 * 0 + 1];
                        min.z = modelList[i].verticesForFaces[3 * 0 + 2];
                        max = min;

                        initMinMax = true;
                    }



                    Int64 j = 0;
                    while (j < modelList[i].noVerticesForFaces)
                    {
                        min.x = Math.Min(min.x, modelList[i].verticesForFaces[6 * j + 0]);
                        min.y = Math.Min(min.y, modelList[i].verticesForFaces[6 * j + 1]);
                        min.z = Math.Min(min.z, modelList[i].verticesForFaces[6 * j + 2]);

                        max.x = Math.Max(max.x, modelList[i].verticesForFaces[6 * j + 0]);
                        max.y = Math.Max(max.y, modelList[i].verticesForFaces[6 * j + 1]);
                        max.z = Math.Max(max.z, modelList[i].verticesForFaces[6 * j + 2]);

                        ++j;
                    }
                }
            }
        }

        private void GetFaceBufferSize(ref Int64 vBuffSize, ref Int64 iBuffSize, int startIndex)
        {
            if (startIndex != 0)
            {
                vBuffSize = modelList[startIndex - 1].vertexOffsetForFaces + modelList[startIndex - 1].noVerticesForFaces;
                iBuffSize = modelList[startIndex - 1].indexOffsetForFaces + 3 * modelList[startIndex - 1].noPrimitivesForFaces;
            }

            for (var i = startIndex; i < modelList.Count; ++i)
            {

                if (modelList[i].ifcID != 0 && modelList[i].noVerticesForFaces != 0 && modelList[i].noPrimitivesForFaces != 0)
                {
                    modelList[i].vertexOffsetForFaces = vBuffSize;
                    modelList[i].indexOffsetForFaces = iBuffSize;

                    vBuffSize += modelList[i].noVerticesForFaces;
                    iBuffSize += 3 * modelList[i].noPrimitivesForFaces;

                    if (i == 0) continue;
                    
                    // 인덱스 오프셋
                    for (var j = 0; j < modelList[i].indicesForFaces.Length; ++j)
                    {
                        modelList[i].indicesForFaces[j] = modelList[i].indicesForFaces[j] + (int)modelList[i].vertexOffsetForFaces;
                    }

                }
            }
        }

       

        private void SetupLights(OpenGL gl)
        {
           
            light1.diffuse  = new vec4(0.7f, 0.7f, 0.7f, 0.1f);
            light1.specular = new vec4(0.7f, 0.7f, 0.7f, 0.1f);
            light1.ambient  = new vec4(0.7f, 0.7f, 0.7f, 0.1f);
            light1.position = new vec3(2.0f, 2.0f, 0.0f);
            vec3 vecDir     = new vec3(-3.0f, -6.0f, -2.0f);
            light1.direction = glm.normalize(vecDir);
            light1.range = 10.0f;

            
            light2.diffuse  = new vec4(0.2f, 0.2f, 0.2f, 1.0f);
            light2.specular = new vec4(0.2f, 0.2f, 0.2f, 1.0f);
            light2.ambient  = new vec4(0.2f, 0.2f, 0.2f, 1.0f);
            light2.position = new vec3(-1.0f, -1.0f, -0.5f);
            vec3 vecDir2    = new vec3(1.0f, 1.0f, 0.5f);
            light2.direction = glm.normalize(vecDir);
            light2.range = 2.0f;

            
            light3.diffuse  = new vec4(0.2f, 0.2f, 0.2f, 1.0f);
            light3.specular = new vec4(0.2f, 0.2f, 0.2f, 1.0f);
            light3.ambient  = new vec4(0.2f, 0.2f, 0.2f, 1.0f);
            light3.position = new vec3(1.0f, 1.0f, 0.5f);
            vec3 vecDir3    = new vec3(-1.0f, -1.0f, -0.5f);
            light3.direction = glm.normalize(vecDir);
            light3.range = 2.0f;
            
        }


        public void Update()
        {
  
        }

        public void Render(OpenGL gl)
        {
            // 장면 클리어
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

            shaderProgram.Bind(gl);
            shaderProgram.SetUniformMatrix4(gl, "matProj", matProj.to_array());
            shaderProgram.SetUniformMatrix4(gl, "matView", matView.to_array());
            shaderProgram.SetUniform3(gl, "dirLight[0].direction", -camera.Look.x, -camera.Look.y, -camera.Look.z);
            shaderProgram.SetUniform3(gl, "dirLight[0].diffuse", light1.diffuse.x, light1.diffuse.y, light1.diffuse.z);
            shaderProgram.SetUniform3(gl, "dirLight[0].ambient", light1.ambient.x, light1.ambient.y, light1.ambient.z);
            shaderProgram.SetUniform3(gl, "dirLight[0].specular", light1.specular.x, light1.specular.y, light1.specular.z);
            shaderProgram.SetUniform3(gl, "dirLight[1].direction", light2.direction.x, light2.direction.y, light2.direction.z);
            shaderProgram.SetUniform3(gl, "dirLight[1].diffuse", light2.diffuse.x, light2.diffuse.y, light2.diffuse.z);
            shaderProgram.SetUniform3(gl, "dirLight[1].ambient", light2.ambient.x, light2.ambient.y, light2.ambient.z);
            shaderProgram.SetUniform3(gl, "dirLight[1].specular", light2.specular.x, light2.specular.y, light2.specular.z);
            shaderProgram.SetUniform3(gl, "dirLight[2].direction", light3.direction.x, light3.direction.y, light3.direction.z);
            shaderProgram.SetUniform3(gl, "dirLight[2].diffuse", light3.diffuse.x, light3.diffuse.y, light3.diffuse.z);
            shaderProgram.SetUniform3(gl, "dirLight[2].ambient", light3.ambient.x, light3.ambient.y, light3.ambient.z);
            shaderProgram.SetUniform3(gl, "dirLight[2].specular", light3.specular.x, light3.specular.y, light3.specular.z);
            

            if (modelList.Count != 0)
            {
                ifcParser.vertexBufferArray.Bind(gl);

                for (var i = 0; i < modelList.Count; ++i)
                {
                    shaderProgram.SetUniform3(gl, "material.ambient",   modelList[i].material.ambient.x, modelList[i].material.ambient.y, modelList[i].material.ambient.z);
                    shaderProgram.SetUniform3(gl, "material.diffuse",   modelList[i].material.diffuse.x, modelList[i].material.diffuse.y, modelList[i].material.diffuse.z);
                    shaderProgram.SetUniform3(gl, "material.specular",  modelList[i].material.specular.x, modelList[i].material.specular.y, modelList[i].material.specular.z);
                    shaderProgram.SetUniform3(gl, "material.emissive", modelList[i].material.emissive.x, modelList[i].material.emissive.y, modelList[i].material.emissive.z);

                    gl.DrawElements(OpenGL.GL_TRIANGLES, 3 * (int)modelList[i].noPrimitivesForFaces, OpenGL.GL_UNSIGNED_INT, IntPtr.Add(IntPtr.Zero, sizeof(int) * (int)modelList[i].indexOffsetForFaces));
                }

                ifcParser.vertexBufferArray.Unbind(gl);
            }

            
            //ifcParser.vertexBufferArray.Bind(gl);
            //gl.DrawElements(OpenGL.GL_TRIANGLES, 6, OpenGL.GL_UNSIGNED_INT, IntPtr.Zero);
            //ifcParser.vertexBufferArray.Unbind(gl);

            shaderProgram.Unbind(gl);

           
        }
    }
}
