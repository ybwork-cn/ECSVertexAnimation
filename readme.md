适用于`Unity``ECS`的GPU顶点动画工具

PS:
- 要求的格式
  - 预制体绑一个`VertexAnimationMapCreator`组件和一个`Animation`组件
  - 预制体有一个骨骼子节点，以及一个或多个`SkinendMeshRenderer`，不支持`MeshFilter`
  - 预制体所有的SkinendMeshRenderer的`Position`、`Rotation`、`Scale`为默认值
  - MaterialChangerComponent可以指定多套材质同一个模型可以出输出多套皮肤的顶点动画
  - SkinendMeshRenderer的材质和输出结果无关，需要在`VertexAnimationMapCreator`中指定输出材质
- 使用方法
  - 在文件内右键点击`ybwork/CreateAllVertexAnimation`按钮，可以递归的将文件夹内所有的模型全部生成顶点动画
  - 顶点动画模型输出目录为`VertexAnimationPrefabs`文件夹
  - 生成结果根据`VertexAnimationMapCreator`中所指定的材质，会生成多套皮肤
  - 生成结果上会有一个`MaterialChangerComponent`组件，里面包含了该皮肤所包含的所有顶点动画动作材质
  - 运行时只需要修改输出预制体的MeshRenderer的Material为`MaterialChangerComponent`中的对应顶点动画动作材质，即可播放对应动画
