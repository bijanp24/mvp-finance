import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { NgxEchartsModule } from 'ngx-echarts';
import type { EChartsOption } from 'echarts';
import { DebtChartData } from '../../core/models/api.models';

@Component({
  selector: 'app-debt-projection-chart',
  imports: [NgxEchartsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div echarts
         [options]="chartOptions()"
         [style.height]="height()"
         role="img"
         [attr.aria-label]="'Debt projection chart showing balance over time'"
         class="chart-container">
    </div>
  `,
  styles: [`
    .chart-container {
      width: 100%;
    }
  `]
})
export class DebtProjectionChartComponent {
  readonly data = input.required<DebtChartData | null>();
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
          const balance = new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD'
          }).format(params[0].value);
          return `${date}<br/>Debt Balance: ${balance}`;
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
          name: 'Total Debt',
          type: 'line',
          data: chartData.debtBalances,
          smooth: true,
          areaStyle: {
            color: 'rgba(244, 67, 54, 0.2)'
          },
          lineStyle: {
            color: '#f44336',
            width: 2
          },
          itemStyle: {
            color: '#f44336'
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
