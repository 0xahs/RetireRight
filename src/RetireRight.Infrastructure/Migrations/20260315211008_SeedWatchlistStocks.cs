using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetireRight.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedWatchlistStocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO stocks (symbol, name, sector, is_active, is_watchlist, lot_size, created_at, updated_at) VALUES
                -- Banking (8)
                ('MCB',     'MCB Bank Limited',                 'Banking',          true, true, 500, NOW(), NOW()),
                ('HBL',     'Habib Bank Limited',               'Banking',          true, true, 500, NOW(), NOW()),
                ('UBL',     'United Bank Limited',              'Banking',          true, true, 500, NOW(), NOW()),
                ('BAHL',    'Bank Al-Habib Limited',            'Banking',          true, true, 500, NOW(), NOW()),
                ('BAFL',    'Bank Alfalah Limited',             'Banking',          true, true, 500, NOW(), NOW()),
                ('ABL',     'Allied Bank Limited',              'Banking',          true, true, 500, NOW(), NOW()),
                ('NBP',     'National Bank of Pakistan',        'Banking',          true, true, 500, NOW(), NOW()),
                ('AKBL',    'Askari Bank Limited',              'Banking',          true, true, 500, NOW(), NOW()),
                -- Islamic Banking (2)
                ('MEBL',    'Meezan Bank Limited',              'IslamicBanking',   true, true, 500, NOW(), NOW()),
                ('FABL',    'Faysal Bank Limited',              'IslamicBanking',   true, true, 500, NOW(), NOW()),
                -- Fertilizer (3)
                ('EFERT',   'Engro Fertilizers Limited',        'Fertilizer',       true, true, 500, NOW(), NOW()),
                ('FFC',     'Fauji Fertilizer Company',         'Fertilizer',       true, true, 500, NOW(), NOW()),
                ('FATIMA',  'Fatima Fertilizer Company',        'Fertilizer',       true, true, 500, NOW(), NOW()),
                -- Oil & Gas E&P (4)
                ('OGDC',    'Oil & Gas Development Company',    'OilAndGas',        true, true, 500, NOW(), NOW()),
                ('POL',     'Pakistan Oilfields Limited',       'OilAndGas',        true, true, 500, NOW(), NOW()),
                ('PPL',     'Pakistan Petroleum Limited',       'OilAndGas',        true, true, 500, NOW(), NOW()),
                ('MARI',    'Mari Petroleum Company',           'OilAndGas',        true, true, 500, NOW(), NOW()),
                -- Oil Marketing (3)
                ('PSO',     'Pakistan State Oil',               'OilAndGas',        true, true, 500, NOW(), NOW()),
                ('SHEL',    'Shell Pakistan Limited',           'OilAndGas',        true, true, 500, NOW(), NOW()),
                ('APL',     'Attock Petroleum Limited',         'OilAndGas',        true, true, 500, NOW(), NOW()),
                -- Power (3)
                ('HUBC',    'Hub Power Company',                'Power',            true, true, 500, NOW(), NOW()),
                ('KAPCO',   'Kot Addu Power Company',           'Power',            true, true, 500, NOW(), NOW()),
                ('NPL',     'Nishat Power Limited',             'Power',            true, true, 500, NOW(), NOW()),
                -- Cement (4)
                ('LUCK',    'Lucky Cement Limited',             'Cement',           true, true, 500, NOW(), NOW()),
                ('DGKC',    'DG Khan Cement Company',           'Cement',           true, true, 500, NOW(), NOW()),
                ('FCCL',    'Fauji Cement Company',             'Cement',           true, true, 500, NOW(), NOW()),
                ('PIOC',    'Pioneer Cement Limited',           'Cement',           true, true, 500, NOW(), NOW()),
                -- Technology (2)
                ('SYS',     'Systems Limited',                  'Technology',       true, true, 500, NOW(), NOW()),
                ('AVN',     'Avanceon Limited',                 'Technology',       true, true, 500, NOW(), NOW()),
                -- Pharmaceutical (4)
                ('SEARL',   'The Searle Company',               'Pharmaceutical',   true, true, 500, NOW(), NOW()),
                ('AGP',     'AGP Limited',                      'Pharmaceutical',   true, true, 500, NOW(), NOW()),
                ('GLAXO',   'GlaxoSmithKline Pakistan',         'Pharmaceutical',   true, true, 500, NOW(), NOW()),
                ('ABOT',    'Abbott Laboratories Pakistan',     'Pharmaceutical',   true, true, 500, NOW(), NOW()),
                -- Food/Consumer (3)
                ('NESTLE',  'Nestle Pakistan Limited',          'Food',             true, true, 500, NOW(), NOW()),
                ('COLG',    'Colgate-Palmolive Pakistan',       'Food',             true, true, 500, NOW(), NOW()),
                ('FFL',     'FrieslandCampina Engro Pakistan',  'Food',             true, true, 500, NOW(), NOW()),
                -- Automobile (3)
                ('INDU',    'Indus Motor Company',              'Automobile',       true, true, 500, NOW(), NOW()),
                ('MTL',     'Millat Tractors Limited',          'Automobile',       true, true, 500, NOW(), NOW()),
                ('PSMC',    'Pak Suzuki Motor Company',         'Automobile',       true, true, 500, NOW(), NOW()),
                -- Textile (2)
                ('NML',     'Nishat Mills Limited',             'Textile',          true, true, 500, NOW(), NOW()),
                ('ILP',     'Interloop Limited',                'Textile',          true, true, 500, NOW(), NOW()),
                -- Chemical (2)
                ('EPCL',    'Engro Polymer & Chemicals',        'Chemical',         true, true, 500, NOW(), NOW()),
                ('LOTCHEM', 'Lotte Chemical Pakistan',          'Chemical',         true, true, 500, NOW(), NOW()),
                -- Insurance (2)
                ('EFUG',    'EFU General Insurance',            'Insurance',        true, true, 500, NOW(), NOW()),
                ('JLIF',    'Jubilee Life Insurance',           'Insurance',        true, true, 500, NOW(), NOW()),
                -- Telecom (1)
                ('PTC',     'Pakistan Telecommunication',       'Telecom',          true, true, 500, NOW(), NOW()),
                -- Conglomerate/Other (3)
                ('ENGRO',   'Engro Corporation Limited',        'Other',            true, true, 500, NOW(), NOW()),
                ('DAWH',    'Dawood Hercules Corporation',      'Other',            true, true, 500, NOW(), NOW()),
                ('ATRL',    'Attock Refinery Limited',          'OilAndGas',        true, true, 500, NOW(), NOW()),
                -- Other (1)
                ('PAEL',    'Pak Elektron Limited',             'Other',            true, true, 500, NOW(), NOW())
                ON CONFLICT (symbol) DO UPDATE SET
                    name = EXCLUDED.name,
                    sector = EXCLUDED.sector,
                    is_watchlist = true,
                    updated_at = NOW();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM stocks WHERE symbol IN (
                    'MCB','HBL','UBL','BAHL','BAFL','ABL','NBP','AKBL',
                    'MEBL','FABL',
                    'EFERT','FFC','FATIMA',
                    'OGDC','POL','PPL','MARI','PSO','SHEL','APL',
                    'HUBC','KAPCO','NPL',
                    'LUCK','DGKC','FCCL','PIOC',
                    'SYS','AVN',
                    'SEARL','AGP','GLAXO','ABOT',
                    'NESTLE','COLG','FFL',
                    'INDU','MTL','PSMC',
                    'NML','ILP',
                    'EPCL','LOTCHEM',
                    'EFUG','JLIF',
                    'PTC',
                    'ENGRO','DAWH','ATRL',
                    'PAEL'
                ) AND NOT EXISTS (
                    SELECT 1 FROM daily_prices dp WHERE dp.stock_id = stocks.id
                );
                """);
        }
    }
}
