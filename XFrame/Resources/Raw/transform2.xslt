<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" indent="yes" encoding="utf-8"/>

  <xsl:key name="employee-key" match="item" use="concat(@name, '|', @surname)" />

  <xsl:template match="/Pay">
    <Employees>
      <xsl:for-each select=".//item[generate-id() = generate-id(key('employee-key', concat(@name, '|', @surname))[1])]">
        <xsl:sort select="@name"/>
        
        <Employee name="{@name}" surname="{@surname}">
          <xsl:for-each select="key('employee-key', concat(@name, '|', @surname))">
            <xsl:sort select="name(..)"/>
            
            <salary>
              <xsl:attribute name="amount">
                <xsl:value-of select="@amount"/>
              </xsl:attribute>
              <xsl:attribute name="mount">
                <xsl:value-of select="name(..)"/>
              </xsl:attribute>
            </salary>
          </xsl:for-each>
        </Employee>
      </xsl:for-each>
    </Employees>
  </xsl:template>
</xsl:stylesheet>