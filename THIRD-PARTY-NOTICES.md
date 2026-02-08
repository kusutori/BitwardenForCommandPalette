# 第三方许可证声明

本文件列出了 Bitwarden For Command Palette 项目中引用或依赖的第三方项目及其许可证信息。

## 项目列表

### 1. Microsoft PowerToys

- **项目链接**: https://github.com/microsoft/PowerToys
- **许可证**: MIT License
- **说明**: Command Palette 扩展框架。本项目基于 PowerToys Command Palette SDK 开发，作为其扩展程序运行。

**MIT License (摘要)**:
```
Copyright (c) Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### 2. Bitwarden

- **项目链接**: https://bitwarden.com/
- **许可证**: MIT License
- **说明**: 开源密码管理器。本项目通过本地 Bitwarden CLI (`bw`) 与 Bitwarden 密码库进行交互。

**MIT License (摘要)**:
```
Copyright (c) Bitwarden Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### 3. bitwarden-cli-bio

- **项目链接**: https://github.com/jeanregisser/bitwarden-cli-bio
- **许可证**: MIT License
- **说明**: Bitwarden CLI with biometric unlock。本项目参考了其生物识别解锁的实现方式。

**MIT License (摘要)**:
```
Copyright (c) Jean Regisser

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## 说明

本项目使用上述第三方项目的方式包括：
- 调用 Bitwarden CLI 工具进行密码库操作
- 基于 PowerToys Command Palette SDK 开发扩展
- 参考 bitwarden-cli-bio 的生物识别解锁实现

所有第三方项目均采用 MIT 许可证，与本项目的 MIT 许可证兼容。

## 完整许可证文本

如需查看完整的许可证文本，请访问各项目的 GitHub 页面或官方网站。

## 本项目许可证

本项目自身采用 MIT 许可证，详见 [LICENSE](LICENSE) 文件。
