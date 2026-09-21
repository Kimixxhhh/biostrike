shader_type canvas_item;
render_mode unshaded;

// 毛玻璃（磨砂）背景：把这层控件「后面」已经画好的画面模糊后当作自己的底色。
// 用法：把本材质挂在一个会画出整块矩形的 Control 上（如 PanelContainer / ColorRect），
// 着色器会完全接管它画出来的颜色 —— 即「底层模糊画面 + 半透明底色」。
//
// 注意：读取屏幕纹理时 Godot 会自动插入一次 back buffer 拷贝，
// 所以本节点必须画在它要模糊的内容之后、前景文字之前（否则会把前景一起糊掉）。

// 屏幕纹理（当前帧已绘制的内容）；repeat_disable 让边缘采样不外绕
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;

// 玻璃底色：Alpha = 面板透明度（0 = 全透明，1 = 不透明）；RGB 按 tint_strength 叠在模糊画面上
uniform vec4 tint : source_color = vec4(0.05, 0.06, 0.09, 0.70);

// 模糊半径（像素，按画布坐标 SCREEN_PIXEL_SIZE 换算，跟随分辨率缩放）
uniform float blur_radius : hint_range(0.0, 48.0) = 16.0;

// 底色 RGB 的叠加强度：0 = 只用模糊画面（清透），1 = 只用底色（浑浊）
uniform float tint_strength : hint_range(0.0, 1.0) = 0.45;

// 采样数：黄金角螺旋均匀铺满圆盘，样本越多越平滑（GPU 开销也越大）
const int SAMPLE_COUNT = 16;
const float GOLDEN_ANGLE = 2.39996323;

void fragment() {
	vec2 radius_px = SCREEN_PIXEL_SIZE * blur_radius;

	vec3 sum = vec3(0.0);
	for (int i = 0; i < SAMPLE_COUNT; i++) {
		float fi = float(i) + 0.5;
		float r = sqrt(fi / float(SAMPLE_COUNT)); // 半径按面积均匀分布，避免中心过密
		float a = fi * GOLDEN_ANGLE;             // 每步转黄金角，避免出现采样网格感
		sum += texture(screen_texture, SCREEN_UV + vec2(cos(a), sin(a)) * r * radius_px).rgb;
	}

	vec3 blurred = sum / float(SAMPLE_COUNT);
	COLOR = vec4(mix(blurred, tint.rgb, tint_strength), tint.a);
}
