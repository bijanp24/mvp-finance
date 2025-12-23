import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { NgxEchartsModule } from 'ngx-echarts';
import type { EChartsOption } from 'echarts';
import { NetWorthChartData } from '../../core/models/api.models';

@Component({
  selector: 'app-net-worth-chart',
  imports: [NgxEchartsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div echarts
         [options]="chartOptions()"
         [style.height]="height()"
         role="img"
         [attr.aria-label]="'Net worth projection chart showing combined portfolio value minus debt over time'"
         class="chart-container">
    </div>
  `,
  styles: [`
    .chart-container {
      width: 100%;
    }
  `]
})
export class NetWorthChartComponent {
  readonly data = input.required<NetWorthChartData | null>();
  readonly height = input<string>('400px');
  readonly compact = input<boolean>(false);

  readonly chartOptions = computed<EChartsOption>(() => {
    const chartData = this.data();
    if (!chartData) return {};

    return {
      tooltip: {
        trigger: 'axis',
        axisPointer: { type: 'cross' },
        formatter: (params: any) => {
          const date = new Date(params[0].name).toLocaleDateString();
          const netWorth = new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD'
          }).format(params[0].value);
          return `${date}<br/>Net Worth: ${netWorth}`;
        }
      },
      xAxis: {
        type: 'category',
        data: chartData.dates,
        axisLabel: {
          formatter: (value: string) => {
            const date = new Date(value);
            return this.compact()
              ? date.toLocaleDateString('en-US', { month: 'short' })
              : date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
          }
        }
      },
      yAxis: {
        type: 'value',
        axisLabel: {
          formatter: (value: number) => {
            return '$' + (value / 1000).toFixed(0) + 'k';
          }
        }
      },
      series: [
        {
          name: 'Net Worth',
          type: 'line',
          data: chartData.netWorth,
          smooth: true,
          areaStyle: {
            color: {
              type: 'linear',
              x: 0,
              y: 0,
              x2: 0,
              y2: 1,
              colorStops: [
                { offset: 0, color: 'rgba(33, 150, 243, 0.3)' },
                { offset: 1, color: 'rgba(33, 150, 243, 0.1)' }
              ]
            }
          },
          lineStyle: {
            color: '#2196f3',
            width: 3
          },
          itemStyle: {
            color: '#2196f3'
          },
          markLine: {
            silent: true,
            symbol: 'none',
            lineStyle: {
              color: '#666',
              type: 'dashed',
              width: 1
            },
            data: [{ yAxis: 0, label: { formatter: '$0' } }]
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

