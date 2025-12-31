import type { ECharts } from 'echarts';

export interface ChartCaptureOptions {
  backgroundColor?: string;
  pixelRatio?: number;
}

export function captureEChartsImage(
  chartInstance: ECharts,
  options: ChartCaptureOptions = {}
): string {
  const { backgroundColor = '#ffffff', pixelRatio = 2 } = options;

  return chartInstance.getDataURL({
    type: 'png',
    pixelRatio,
    backgroundColor
  });
}

export function captureMultipleCharts(
  charts: ECharts[],
  options: ChartCaptureOptions = {}
): string[] {
  return charts
    .filter(chart => chart != null)
    .map(chart => captureEChartsImage(chart, options));
}
