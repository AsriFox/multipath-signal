namespace MultipathSignal.Views;
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using static Avalonia.OpenGL.GlConsts;

public class OpenGlPageControl : OpenGlControlBase
{
    private float _yaw;

    public static readonly DirectProperty<OpenGlPageControl, float> YawProperty =
        AvaloniaProperty.RegisterDirect<OpenGlPageControl, float>(nameof(Yaw), o => o.Yaw, (o, v) => o.Yaw = v);

    public float Yaw
    {
        get => _yaw;
        set => SetAndRaise(YawProperty, ref _yaw, value);
    }

    private float _pitch = 5.0f;

    public static readonly DirectProperty<OpenGlPageControl, float> PitchProperty =
        AvaloniaProperty.RegisterDirect<OpenGlPageControl, float>(nameof(Pitch), o => o.Pitch, (o, v) => o.Pitch = v);

    public float Pitch
    {
        get => _pitch;
        set => SetAndRaise(PitchProperty, ref _pitch, value);
    }


    private float _roll;

    public static readonly DirectProperty<OpenGlPageControl, float> RollProperty =
        AvaloniaProperty.RegisterDirect<OpenGlPageControl, float>(nameof(Roll), o => o.Roll, (o, v) => o.Roll = v);

    public float Roll
    {
        get => _roll;
        set => SetAndRaise(RollProperty, ref _roll, value);
    }


    private float _distance = 2.0f;

    public static readonly DirectProperty<OpenGlPageControl, float> DistanceProperty =
        AvaloniaProperty.RegisterDirect<OpenGlPageControl, float>(nameof(Distance), o => o.Distance, (o, v) => o.Distance = v);

    public float Distance
    {
        get => _distance;
        set => SetAndRaise(DistanceProperty, ref _distance, value);
    }

    private string _info = string.Empty;

    public static readonly DirectProperty<OpenGlPageControl, string> InfoProperty =
        AvaloniaProperty.RegisterDirect<OpenGlPageControl, string>(nameof(Info), o => o.Info, (o, v) => o.Info = v);

    public string Info
    {
        get => _info;
        private set => SetAndRaise(InfoProperty, ref _info, value);
    }

    static OpenGlPageControl()
    {
        AffectsRender<OpenGlPageControl>(YawProperty, PitchProperty, RollProperty, DistanceProperty);
    }

    private int _vertexShader;
    private int _fragmentShader;
    private int _shaderProgram;
    private int _vertexBufferObject;
    private int _indexBufferObject;
    private int _vertexArrayObject;
    private GlExtrasInterface? _glExt;

    private string GetShader(bool fragment, string shader)
    {
        var version = (GlVersion.Type == GlProfileType.OpenGL ?
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 150 : 120 :
            100);
        var data = "#version " + version + "\n";
        if (GlVersion.Type == GlProfileType.OpenGLES)
            data += "precision mediump float;\n";
        if (version >= 150)
        {
            shader = shader.Replace("attribute", "in");
            if (fragment)
                shader = shader
                    .Replace("varying", "in")
                    .Replace("//DECLAREGLFRAG", "out vec4 outFragColor;")
                    .Replace("gl_FragColor", "outFragColor");
            else
                shader = shader.Replace("varying", "out");
        }

        data += shader;

        return data;
    }


    private string VertexShaderSource => GetShader(false, @"
    attribute vec3 aPos;
    attribute vec3 aNormal;
    uniform mat4 uModel;
    uniform mat4 uProjection;
    uniform mat4 uView;

    varying vec3 FragPos;
    varying vec3 VecPos;  
    varying vec3 Normal;
    void main()
    {
        gl_Position = uProjection * uView * uModel * vec4(aPos, 1.0);
        FragPos = vec3(uModel * vec4(aPos, 1.0));
        VecPos = aPos;
        Normal = normalize(vec3(uModel * vec4(aNormal, 1.0)));
    }
");

    private string FragmentShaderSource => GetShader(true, @"
    varying vec3 FragPos; 
    varying vec3 VecPos; 
    varying vec3 Normal;
    uniform float uMaxZ;
    uniform float uMinZ;
    //DECLAREGLFRAG

    void main()
    {
        float z = (VecPos.z - uMinZ) / (uMaxZ - uMinZ);
        vec3 objectColor = vec3((1.0 - z), 0.40 +  z / 4.0, z * 0.75 + 0.25);

        float ambientStrength = 0.3;
        vec3 lightColor = vec3(1.0, 1.0, 1.0);
        vec3 lightPos = vec3(uMaxZ * 2.0, uMaxZ * 2.0, uMaxZ * 2.0);
        vec3 ambient = ambientStrength * lightColor;

        vec3 norm = normalize(Normal);
        vec3 lightDir = normalize(lightPos - FragPos);  

        float diff = max(dot(norm, lightDir), 0.0);
        vec3 diffuse = diff * lightColor;

        vec3 result = (ambient + diffuse) * objectColor;
        gl_FragColor = vec4(result, 1.0);
    }
");

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
    }

    private Vertex[] _points = Array.Empty<Vertex>();
    private ushort[] _indices = Array.Empty<ushort>();
    private float _minZ;
    private float _maxZ;


    public OpenGlPageControl()
    {
        this.Initialized += Init;
    }

    public void Init(object? sender, EventArgs e)
    {
        if (this.Parent?.Parent is not OpenGlPage page) {
            throw new NullReferenceException();
        }
        var points = page.points;
        _indices = page.indices.ToArray();
        _points = points.Select(
            p => new Vertex { Position = p }
        ).ToArray();

        // Calculate normals
        for (int i = 0; i < _indices.Length; i += 3)
        {
            Vector3 a = _points[_indices[i]].Position;
            Vector3 b = _points[_indices[i + 1]].Position;
            Vector3 c = _points[_indices[i + 2]].Position;
            var normal = Vector3.Normalize(Vector3.Cross(c - b, a - b));

            _points[_indices[i]].Normal += normal;
            _points[_indices[i + 1]].Normal += normal;
            _points[_indices[i + 2]].Normal += normal;
        }

        // Normalize normals and find min/max
        for (int i = 0; i < _points.Length; i++)
        {
            _points[i].Normal = Vector3.Normalize(_points[i].Normal);
            _maxZ = Math.Max(_maxZ, _points[i].Position.Z);
            _minZ = Math.Min(_minZ, _points[i].Position.Z);
        }
    }

    private void CheckError(GlInterface gl)
    {
        int err;
        while ((err = gl.GetError()) != GL_NO_ERROR)
            Console.WriteLine(err);
    }

    protected unsafe override void OnOpenGlInit(GlInterface GL, int fb)
    {
        CheckError(GL);
        _glExt = new GlExtrasInterface(GL);

        Info = $"Renderer: {GL.GetString(GL_RENDERER)} Version: {GL.GetString(GL_VERSION)}";
        
        // Load the source of the vertex shader and compile it.
        _vertexShader = GL.CreateShader(GL_VERTEX_SHADER);
        Console.WriteLine(GL.CompileShaderAndGetError(_vertexShader, VertexShaderSource));

        // Load the source of the fragment shader and compile it.
        _fragmentShader = GL.CreateShader(GL_FRAGMENT_SHADER);
        Console.WriteLine(GL.CompileShaderAndGetError(_fragmentShader, FragmentShaderSource));

        // Create the shader program, attach the vertex and fragment shaders and link the program.
        _shaderProgram = GL.CreateProgram();
        GL.AttachShader(_shaderProgram, _vertexShader);
        GL.AttachShader(_shaderProgram, _fragmentShader);
        const int positionLocation = 0;
        const int normalLocation = 1;
        GL.BindAttribLocationString(_shaderProgram, positionLocation, "aPos");
        GL.BindAttribLocationString(_shaderProgram, normalLocation, "aNormal");
        Console.WriteLine(GL.LinkProgramAndGetError(_shaderProgram));
        CheckError(GL);

        // Create the vertex buffer object (VBO) for the vertex data.
        _vertexBufferObject = GL.GenBuffer();
        // Bind the VBO and copy the vertex data into it.
        GL.BindBuffer(GL_ARRAY_BUFFER, _vertexBufferObject);
        CheckError(GL);
        var vertexSize = Marshal.SizeOf<Vertex>();
        fixed (void* pdata = _points)
            GL.BufferData(GL_ARRAY_BUFFER, new IntPtr(_points.Length * vertexSize),
                new IntPtr(pdata), GL_STATIC_DRAW);

        _indexBufferObject = GL.GenBuffer();
        GL.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, _indexBufferObject);
        CheckError(GL);
        fixed (void* pdata = _indices)
            GL.BufferData(GL_ELEMENT_ARRAY_BUFFER, new IntPtr(_indices.Length * sizeof(ushort)), new IntPtr(pdata),
                GL_STATIC_DRAW);
        CheckError(GL);
        _vertexArrayObject = _glExt.GenVertexArray();
        _glExt.BindVertexArray(_vertexArrayObject);
        CheckError(GL);
        GL.VertexAttribPointer(positionLocation, 3, GL_FLOAT,
            0, vertexSize, IntPtr.Zero);
        GL.VertexAttribPointer(normalLocation, 3, GL_FLOAT,
            0, vertexSize, new IntPtr(12));
        GL.EnableVertexAttribArray(positionLocation);
        GL.EnableVertexAttribArray(normalLocation);
        CheckError(GL);

    }

    protected override void OnOpenGlDeinit(GlInterface GL, int fb)
    {
        // Unbind everything
        GL.BindBuffer(GL_ARRAY_BUFFER, 0);
        GL.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, 0);
        _glExt?.BindVertexArray(0);
        GL.UseProgram(0);

        // Delete all resources.
        GL.DeleteBuffers(2, new[] { _vertexBufferObject, _indexBufferObject });
        _glExt?.DeleteVertexArrays(1, new[] { _vertexArrayObject });
        GL.DeleteProgram(_shaderProgram);
        GL.DeleteShader(_fragmentShader);
        GL.DeleteShader(_vertexShader);
    }

    static Stopwatch St = Stopwatch.StartNew();
    protected override unsafe void OnOpenGlRender(GlInterface gl, int fb)
    {
        gl.ClearColor(0, 0, 0, 0);
        gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        gl.Enable(GL_DEPTH_TEST);
        gl.Viewport(0, 0, (int)Bounds.Width, (int)Bounds.Height);
        var GL = gl;

        GL.BindBuffer(GL_ARRAY_BUFFER, _vertexBufferObject);
        GL.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, _indexBufferObject);
        _glExt?.BindVertexArray(_vertexArrayObject);
        GL.UseProgram(_shaderProgram);
        CheckError(GL);
        var projection =
            Matrix4x4.CreatePerspectiveFieldOfView((float)(Math.PI / 4), (float)(Bounds.Width / Bounds.Height),
                0.01f, 1000);


        var view = Matrix4x4.CreateLookAt(
            new Vector3(Distance, Distance, Distance), 
            new Vector3(), 
            new Vector3(0, -1, 0)
        );
        var model = Matrix4x4.CreateFromYawPitchRoll(_yaw, _pitch, _roll);
        var modelLoc = GL.GetUniformLocationString(_shaderProgram, "uModel");
        var viewLoc = GL.GetUniformLocationString(_shaderProgram, "uView");
        var projectionLoc = GL.GetUniformLocationString(_shaderProgram, "uProjection");
        var maxZLoc = GL.GetUniformLocationString(_shaderProgram, "uMaxZ");
        var minZLoc = GL.GetUniformLocationString(_shaderProgram, "uMinZ");
        GL.UniformMatrix4fv(modelLoc, 1, false, &model);
        GL.UniformMatrix4fv(viewLoc, 1, false, &view);
        GL.UniformMatrix4fv(projectionLoc, 1, false, &projection);
        GL.Uniform1f(maxZLoc, _maxZ);
        GL.Uniform1f(minZLoc, _minZ);
        CheckError(GL);
        GL.DrawElements(GL_TRIANGLES, _indices.Length, GL_UNSIGNED_SHORT, IntPtr.Zero);

        CheckError(GL);
        if (_distance > 0.01)
            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
    }

#pragma warning disable CS8618
    class GlExtrasInterface : GlInterfaceBase<GlInterface.GlContextInfo>
    {
        public GlExtrasInterface(GlInterface gl) : base(gl.GetProcAddress, gl.ContextInfo)
        {
        }
        
        public delegate void GlDeleteVertexArrays(int count, int[] buffers);
        [GlMinVersionEntryPoint("glDeleteVertexArrays", 3,0)]
        [GlExtensionEntryPoint("glDeleteVertexArraysOES", "GL_OES_vertex_array_object")]
        public GlDeleteVertexArrays DeleteVertexArrays { get; }
        
        public delegate void GlBindVertexArray(int array);
        [GlMinVersionEntryPoint("glBindVertexArray", 3,0)]
        [GlExtensionEntryPoint("glBindVertexArrayOES", "GL_OES_vertex_array_object")]
        public GlBindVertexArray BindVertexArray { get; }
        public delegate void GlGenVertexArrays(int n, int[] rv);
    
        [GlMinVersionEntryPoint("glGenVertexArrays",3,0)]
        [GlExtensionEntryPoint("glGenVertexArraysOES", "GL_OES_vertex_array_object")]
        public GlGenVertexArrays GenVertexArrays { get; }
        
        public int GenVertexArray()
        {
            var rv = new int[1];
            GenVertexArrays(1, rv);
            return rv[0];
        }
    }
}
