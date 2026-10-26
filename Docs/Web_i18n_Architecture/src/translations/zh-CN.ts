/**
 * ========================================================================
 * Date: 26/10/2569 (BE) / 26/10/2026 (CE)
 * File: zh-CN.ts
 * Version: 1.0.0
 * Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
 * [ Original by DELTA SYNTH ]
 * Location: Docs/Web_i18n_Architecture/src/translations/zh-CN.ts
 * Description: Simplified Chinese (简体中文) Translation Dictionary
 * ========================================================================
 */

import { TranslationSchema } from "../models/i18n.types";

export const zh_CN: TranslationSchema = {
  app: {
    title: "DELTA SYNTH 语音工作室",
    subtitle: "企业级神经歌声与语音合成平台",
    version: "版本 35.1.7",
    tagline: "依托现代架构，实现极速、低延迟的高性能音频智能"
  },
  nav: {
    home: "首页",
    studio: "制作工作室",
    synthesis: "语音合成与DSP",
    models: "声线模型与数据集",
    settings: "系统设置",
    docs: "开发文档与API",
    community: "社区",
    language: "语言 / 地区"
  },
  studio: {
    projectTitle: "项目名称",
    track: "人声音轨",
    tempo: "曲速 (BPM)",
    timeSignature: "节拍记号",
    play: "播放",
    pause: "暂停",
    stop: "停止",
    render: "渲染音频",
    rendering: "正在合成人声表演...",
    exportWav: "导出WAV音频",
    importMidi: "导入MIDI / MusicXML",
    timeline: "音符时间轴",
    notesCount: "音符总数"
  },
  synthesis: {
    pitchShift: "变调 (半音单位)",
    f0Method: "音高提取算法 (F0 Method)",
    indexRate: "特征检索索引率 (Index Rate)",
    protectVoice: "清辅音与呼吸保护",
    resampler: "重采样引擎 (Resampler)",
    batchConvert: "批量音频转换",
    renderStatus: "处理状态",
    vocalQuality: "音色质量",
    autoPitchVibrato: "自动音高与自然颤音"
  },
  telemetry: {
    cpu: "CPU 使用率",
    vram: "显存 / VRAM 占用",
    latency: "实时延迟",
    epoch: "当前周期 (Epoch)",
    bufferSize: "音频缓冲区大小",
    statusReady: "系统就绪",
    statusBusy: "正在进行高速DSP运算"
  },
  settings: {
    title: "系统与引擎偏好设置",
    language: "界面语言",
    theme: "主题 (深色 / 浅色)",
    audioDriver: "音频输出驱动",
    sampleRate: "音频采样率",
    saveSuccess: "配置已成功保存",
    resetDefaults: "恢复出厂默认值"
  },
  dialog: {
    confirm: "确认",
    cancel: "取消",
    delete: "删除",
    warning: "重要警告",
    close: "关闭"
  },
  toast: {
    renderSuccess: "音频渲染成功完成！",
    renderError: "合成失败，请检查显卡或显存日志。",
    languageChanged: "界面语言已成功同步为“简体中文”。",
    fileLoaded: "项目文件加载完成。"
  },
  validation: {
    required: "此项为必填项。",
    invalidNumber: "请输入有效的数值。",
    fileTooLarge: "上传文件超出最大限制。"
  }
};
