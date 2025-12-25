import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { NgxEchartsModule } from 'ngx-echarts';
import type { EChartsOption } from 'echarts';
import { InvestmentChartData } from '../../core/models/api.models';

@Component({
  selector: 'app-investment-projection-chart',
  imports: [NgxEchartsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div echarts
         [options]="chartOptions()"
         [style.height]="height()"
         role="img"
         [attr.aria-label]="'Investment projection chart showing portfolio growth over time'"
         class="chart-container">
    </div>
  `,
  styles: [`
    .chart-container {
      width: 100%;
    }
  `]
})
export class InvestmentProjectionChartComponent {
  readonly data = input.required<InvestmentChartData | null>();
  readonly height = input<string>('400px');
  readonly compact = input<boolean>(false);

  readonly chartOptions = computed<EChartsOption>(() => {
    const chartData = this.data();
    if (!chartData) return {};

    return {
      tooltip: {
        trigger: 'axis',
        formatter: (params: any) => {
          const date = new Date(params[0].name).toLocaleDateString();
          const value = new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD'
          }).format(params[0].value);
          return `${date}<br/>Portfolio Value: ${value}`;
        }
      },
      xAxis: {
        type: 'category',
        data: chartData.dates,
        axisLabel: {
          color: '#94a3b8',
          formatter: (value: string) => {
            const date = new Date(value);
            return this.compact()
              ? date.toLocaleDateString('en-US', { month: 'short' })
              : date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
          }
        },
        axisLine: { lineStyle: { color: '#334155' } }
      },
      yAxis: {
        type: 'value',
        axisLabel: {
          color: '#94a3b8',
          formatter: (value: number) => {
            return '$' + (value / 1000).toFixed(0) + 'k';
          }
        },
        splitLine: { lineStyle: { color: '#1e293b' } }
      },
      series: [
        {
          name: 'Portfolio Value',
          type: 'line',
          data: chartData.values,
          smooth: true,
          showSymbol: false,
          areaStyle: {
            color: {
              type: 'linear',
              x: 0, y: 0, x2: 0, y2: 1,
              colorStops: [
                { offset: 0, color: 'rgba(74, 222, 128, 0.2)' },
                { offset: 1, color: 'rgba(74, 222, 128, 0)' }
              ]
            }
          },
          lineStyle: {
            color: '#4ade80',
            width: 3
          },
          itemStyle: {
            color: '#4ade80'
          }
        }
      ],
      dataZoom: this.compact() ? [] : [
        { type: 'inside' },
        { type: 'slider', height: 20 }
      ],
      grid: {
        left: this.compact() ? '5%' : '10%',
        right: this.compact() ? '5%' : '10%',
        bottom: this.compact() ? '5%' : '15%',
        top: '10%',
        containLabel: true
      }
    };
  });
}
