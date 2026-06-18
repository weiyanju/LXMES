# Git 日常流程

项目目录：

```powershell
cd C:\Users\Yangqf\source\repos\VfdProductionControl
```

## 第一次初始化项目

```powershell
git init
git branch -M main
```

如果已经在 GitHub 创建了空仓库，关联远程仓库：

```powershell
git remote add origin https://github.com/你的用户名/VfdProductionControl.git
```

第一次提交并推送：

```powershell
git add .
git commit -m "Initial commit"
git push -u origin main
```

## 每天开始工作

```powershell
cd C:\Users\Yangqf\source\repos\VfdProductionControl
git status
git pull
```

## 查看改了什么

```powershell
git status
git diff
```

## 提交本次修改

```powershell
git add .
git commit -m "说明这次改了什么"
```

示例：

```powershell
git commit -m "Update workflow design"
```

## 推送到 GitHub

```powershell
git push
```

## 完整日常流程

```powershell
cd C:\Users\Yangqf\source\repos\VfdProductionControl
git pull
git status

# 修改代码或文档后
git status
git diff
git add .
git commit -m "Update workflow design"
git push
```

## 查看提交历史

```powershell
git log --oneline
```

## 只提交某些文件

```powershell
git add README.md
git add docs\需求文档.md
git commit -m "Update docs"
git push
```

## 最常用的 5 个命令

```powershell
git status
git pull
git add .
git commit -m "你的说明"
git push
```

## 上传 GitHub 前的提醒

第一次上传前，建议先添加 `.gitignore`，避免把以下内容提交到 GitHub：

- `bin/`
- `obj/`
- `.vs/`
- 日志文件
- 本机临时文件
- 真实数据库连接字符串和密码

