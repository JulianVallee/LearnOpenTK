using LearnOpenTK.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;

namespace LearnOpenTK
{
    /**
     * OpenTK port of https://github.com/JoeyDeVries/LearnOpenGL/blob/master/src/5.advanced_lighting/4.normal_mapping
     * 
     * Differences to the original
     * 
     *  - UVs are scaled by "_quadSize" so the texture repeats instead of stretching over the whole plane, see "PrepareRenderQuad()"
     *  - Diffuse lighting value got multiplied by 100 to pronounce the diffuse lighting issue
     *  - Added new uniform "isLight" to the fragment shader, used to render the light with a color instead of the used texture
     *  
     *  
     * Issue description
     * 
     *    The diffuse point light is not uniformly lighting a circle, but instead appears to be distorted to the approx. direction of [0.5, 0.5, 0]
     * 
     *
     * Things I have tried so far
     * 
     *  - Using the hardcoded tangents (in this demo)
     *  - Using tangents calculated by assimp (in other project)
     *  - Using tangents based on the method "computeTangentBasis()" in http://www.opengl-tutorial.org/intermediate-tutorials/tutorial-13-normal-mapping/ (in other project)
     *  - Calculating TBN matrix both in vertex and fragment shader
     *  - Transpose/invert matrices and hope for the best
     */
    public class Window : GameWindow
    {
        // Scaling factor for the normal mapped quad, readonly as texture will be resized on initial setup
        private readonly float _quadSize = 1000.0f;

        private Vector3 _lightPos = new Vector3(0.0f, 0.0f, 1f);

        private int _quadVAO = 0;

        private int _quadVBO;

        private Shader _normalMappingShader;

        private Texture _diffuseMap;

        private Texture _normalMap;

        private Camera _camera;

        private bool _firstMove = true;

        private Vector2 _lastPos;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            // Configure global opengl state
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            // Build and compile shaders
            _normalMappingShader = new Shader("Shaders/normal_mapping.vert", "Shaders/normal_mapping.frag");

            // Load textures
            _diffuseMap = new Texture("Resources/brickwall.jpg");
            _normalMap = new Texture("Resources/brickwall_normal.jpg");

            // Shader configuration
            _normalMappingShader.Use();
            _normalMappingShader.SetInt("diffuseMap", 0);
            _normalMappingShader.SetInt("normalMap", 1);

            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);

            CursorGrabbed = true;

            base.OnLoad();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Render plane
            Matrix4 model = Matrix4.Identity;
            model *= Matrix4.CreateScale(Vector3.One * _quadSize);
            model *= Matrix4.CreateTranslation(Vector3.Zero);

            _normalMappingShader.Use();
            _normalMappingShader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            _normalMappingShader.SetMatrix4("view", _camera.GetViewMatrix());
            _normalMappingShader.SetMatrix4("model", model);
            _normalMappingShader.SetVector3("viewPos", _camera.Position);
            _normalMappingShader.SetVector3("lightPos", _lightPos);
            _normalMappingShader.SetInt("isLight", 0);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _diffuseMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, _normalMap.Handle);
            RenderQuad();

            // Render light for visualization
            model = Matrix4.Identity;
			model *= Matrix4.CreateScale(new Vector3(0.1f));
			model *= Matrix4.CreateTranslation(_lightPos);

			_normalMappingShader.SetMatrix4("model", model);
            _normalMappingShader.SetInt("isLight", 1);
            RenderQuad();

            // Mandatory buffer swap
			SwapBuffers();

            base.OnRenderFrame(e);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (!IsFocused)
            {
                return;
            }

            var input = KeyboardState;

            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            const float cameraSpeed = 15f;
            const float sensitivity = 0.2f;

            if (input.IsKeyDown(Keys.W))
            {
                _camera.Position += _camera.Front * cameraSpeed * (float)e.Time; // Forward
            }
            if (input.IsKeyDown(Keys.S))
            {
                _camera.Position -= _camera.Front * cameraSpeed * (float)e.Time; // Backwards
            }
            if (input.IsKeyDown(Keys.A))
            {
                _camera.Position -= _camera.Right * cameraSpeed * (float)e.Time; // Left
            }
            if (input.IsKeyDown(Keys.D))
            {
                _camera.Position += _camera.Right * cameraSpeed * (float)e.Time; // Right
            }
            if (input.IsKeyDown(Keys.Space))
            {
                _camera.Position += _camera.Up * cameraSpeed * (float)e.Time; // Up
            }
            if (input.IsKeyDown(Keys.LeftShift))
            {
                _camera.Position -= _camera.Up * cameraSpeed * (float)e.Time; // Down
            }

            var mouse = MouseState;

            if (_firstMove)
            {
                _lastPos = new Vector2(mouse.X, mouse.Y);
                _firstMove = false;
            }
            else
            {
                var deltaX = mouse.X - _lastPos.X;
                var deltaY = mouse.Y - _lastPos.Y;
                _lastPos = new Vector2(mouse.X, mouse.Y);

                _camera.Yaw += deltaX * sensitivity;
                _camera.Pitch -= deltaY * sensitivity;
            }

            base.OnUpdateFrame(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            _camera.Fov -= e.OffsetY;
            base.OnMouseWheel(e);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            GL.Viewport(0, 0, Size.X, Size.Y);
            _camera.AspectRatio = Size.X / (float)Size.Y;
            base.OnResize(e);
        }

        protected override void OnUnload()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);

            GL.DeleteBuffer(_quadVBO);
            GL.DeleteVertexArray(_quadVAO);

            GL.DeleteProgram(_normalMappingShader.Handle);

            base.OnUnload();
        }

        private void PrepareRenderQuad()
        {
            // Plane vertice positions
            Vector3[] vertices =
            {
                new Vector3(-1.0f, 1.0f, 0.0f),
                new Vector3(-1.0f, -1.0f, 0.0f),
                new Vector3(1.0f, -1.0f, 0.0f),
                new Vector3(1.0f, 1.0f, 0.0f)
            };

            // Plane texture coordinates
            Vector2[] uvs =
            {
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 0.0f),
                new Vector2(1.0f, 1.0f)
            };

            // Plane normal vector
            Vector3 normal = new Vector3(0.0f, 0.0f, 1.0f);

            // Scale UVs relative to the quad size
            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i] = uvs[i] * _quadSize;
            }

            // Triangle 1
            Vector3 edge1 = vertices[1] - vertices[0];
            Vector3 edge2 = vertices[2] - vertices[0];
            Vector2 deltaUV1 = uvs[1] - uvs[0];
            Vector2 deltaUV2 = uvs[2] - uvs[0];

            float f = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y);

            Vector3 tangent1 = new Vector3();
            tangent1.X = f * (deltaUV2.Y * edge1.X - deltaUV1.Y * edge2.X);
            tangent1.Y = f * (deltaUV2.Y * edge1.Y - deltaUV1.Y * edge2.Y);
            tangent1.Z = f * (deltaUV2.Y * edge1.Z - deltaUV1.Y * edge2.Z);

            Vector3 bitangent1 = new Vector3();
            bitangent1.X = f * (-deltaUV2.X * edge1.X + deltaUV1.X * edge2.X);
            bitangent1.Y = f * (-deltaUV2.X * edge1.Y + deltaUV1.X * edge2.Y);
            bitangent1.Z = f * (-deltaUV2.X * edge1.Z + deltaUV1.X * edge2.Z);

            // Triangle 2
            edge1 = vertices[2] - vertices[0];
            edge2 = vertices[3] - vertices[0];
            deltaUV1 = uvs[2] - uvs[0];
            deltaUV2 = uvs[3] - uvs[0];

            f = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y);

            Vector3 tangent2 = new Vector3();
            tangent2.X = f * (deltaUV2.Y * edge1.X - deltaUV1.Y * edge2.X);
            tangent2.Y = f * (deltaUV2.Y * edge1.Y - deltaUV1.Y * edge2.Y);
            tangent2.Z = f * (deltaUV2.Y * edge1.Z - deltaUV1.Y * edge2.Z);

            Vector3 bitangent2 = new Vector3();
            bitangent2.X = f * (-deltaUV2.X * edge1.X + deltaUV1.X * edge2.X);
            bitangent2.Y = f * (-deltaUV2.X * edge1.Y + deltaUV1.X * edge2.Y);
            bitangent2.Z = f * (-deltaUV2.X * edge1.Z + deltaUV1.X * edge2.Z);

            float[] quadVertices = new float[] {
                // Vertices                                  // Normals                    // Texcoords        // Tangents                         // Bitangents
                vertices[0].X, vertices[0].Y, vertices[0].Z, normal.X, normal.Y, normal.Z, uvs[0].X, uvs[0].Y, tangent1.X, tangent1.Y, tangent1.Z, bitangent1.X, bitangent1.Y, bitangent1.Z,
                vertices[1].X, vertices[1].Y, vertices[1].Z, normal.X, normal.Y, normal.Z, uvs[1].X, uvs[1].Y, tangent1.X, tangent1.Y, tangent1.Z, bitangent1.X, bitangent1.Y, bitangent1.Z,
                vertices[2].X, vertices[2].Y, vertices[2].Z, normal.X, normal.Y, normal.Z, uvs[2].X, uvs[2].Y, tangent1.X, tangent1.Y, tangent1.Z, bitangent1.X, bitangent1.Y, bitangent1.Z,

                vertices[0].X, vertices[0].Y, vertices[0].Z, normal.X, normal.Y, normal.Z, uvs[0].X, uvs[0].Y, tangent2.X, tangent2.Y, tangent2.Z, bitangent2.X, bitangent2.Y, bitangent2.Z,
                vertices[2].X, vertices[2].Y, vertices[2].Z, normal.X, normal.Y, normal.Z, uvs[2].X, uvs[2].Y, tangent2.X, tangent2.Y, tangent2.Z, bitangent2.X, bitangent2.Y, bitangent2.Z,
                vertices[3].X, vertices[3].Y, vertices[3].Z, normal.X, normal.Y, normal.Z, uvs[3].X, uvs[3].Y, tangent2.X, tangent2.Y, tangent2.Z, bitangent2.X, bitangent2.Y, bitangent2.Z
            };

            // Configure plane VAO
            _quadVAO = GL.GenVertexArray();
            _quadVBO = GL.GenBuffer();
            GL.BindVertexArray(_quadVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);

            var positionLocation = _normalMappingShader.GetAttribLocation("aPos");
            GL.EnableVertexAttribArray(positionLocation);
            GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 0);

            var normalLocation = _normalMappingShader.GetAttribLocation("aNormal");
            GL.EnableVertexAttribArray(normalLocation);
            GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 3 * sizeof(float));

            var texCoordLocation = _normalMappingShader.GetAttribLocation("aTexCoords");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 14 * sizeof(float), 6 * sizeof(float));

            var tangentLocation = _normalMappingShader.GetAttribLocation("aTangent");
            GL.EnableVertexAttribArray(tangentLocation);
            GL.VertexAttribPointer(tangentLocation, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 8 * sizeof(float));

            var bitangentLocation = _normalMappingShader.GetAttribLocation("aBitangent");
            GL.EnableVertexAttribArray(bitangentLocation);
            GL.VertexAttribPointer(bitangentLocation, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 11 * sizeof(float));
        }

        private void RenderQuad()
        {
            if(_quadVAO == 0)
			{
                PrepareRenderQuad();
            }

            GL.BindVertexArray(_quadVAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.BindVertexArray(0);
        }
    }
}